import { HttpClient } from '@angular/common/http';
import { EventEmitter, Inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { HttpInterceptorService } from './http-interceptor.service';
import { ConnectionStatus, WebSocketMessage, WebsocketService } from './websocket.service';
import * as idb from 'idb/with-async-ittr';
import { IDBPDatabase } from 'idb/with-async-ittr';
import { SortDirection } from '@angular/material/sort';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AppService {
  public user: User | null = null;
  public onUserChanged: EventEmitter<void> = new EventEmitter<void>();

  public httpLoading = false;
  public webSocketLoading = false;

  public networkLoading = false;
  public networkLoadingChanged: EventEmitter<boolean> = new EventEmitter<boolean>();

  private settingsDb: IDBPDatabase<SettingsDb> | null = null;
  private settingsDbReady: Promise<void> | null = null;

  public constructor(
    private readonly httpClient: HttpClient,
    @Inject('API_URL') private readonly apiUrl: string,
    private readonly router: Router,
    private readonly webSocketService: WebsocketService,
    private readonly httpInterceptorService: HttpInterceptorService) {
    this.webSocketService.onConnectionStatusChanged.subscribe((connectionStatus: ConnectionStatus) => {
      if (connectionStatus === ConnectionStatus.Connected && this.webSocketService.connectionIsAuthenticated) {
        this.webSocketService.sendMessage("CommanderMe", {});
      }
      else if (connectionStatus !== ConnectionStatus.Connected) {
        this.webSocketLoading = true;
      }
    });
    this.webSocketService.on<User>("CommanderMe").subscribe((message: WebSocketMessage<User>) => {
      this.user = message.Data;
      this.onUserChanged.emit();
    });
    this.httpInterceptorService.loadingSub.subscribe((loading: boolean) => {
      this.httpLoading = loading;
      this.updateNetworkLoading();
    });
    this.webSocketService.messageBacklogChanged.subscribe((backlog: number) => {
      this.webSocketLoading = backlog > 0;
      this.updateNetworkLoading();
    });
    this.initDb();
  }

  private async initDb(): Promise<void> {
    let readyResolve: ((a: void) => void);
    this.settingsDbReady = new Promise((resolve) => {
      readyResolve = resolve;
    });
    try {
      this.settingsDb = await idb.openDB<SettingsDb>('OverwatchSettings', 1, {
        upgrade(db) {
          db.createObjectStore('TableSort');
          db.createObjectStore('Settings');
        },
      });
    }
    catch (e) {
      console.error(e);
      this.settingsDb = null;
    }
    if (readyResolve!) {
      readyResolve();
      this.settingsDbReady = null;
    }
  }

  public async getTableSort(name: string, defaultColumn: string, defaultDirection: SortDirection = "asc"): Promise<TableSortSetting> {
    if (this.settingsDbReady) {
      await this.settingsDbReady;
    }
    if (this.settingsDb) {
      const tableSortSettings = await this.settingsDb.get('TableSort', name);
      if (tableSortSettings) {
        return tableSortSettings;
      }
    }
    return {
      Column: defaultColumn,
      Direction: defaultDirection,
    };
  }

  public async updateTableSort(name: string, column: string, direction: SortDirection): Promise<void> {
    if (this.settingsDb) {
      const data: TableSortSetting = {
        Column: column,
        Direction: direction,
      };
      await this.settingsDb.put('TableSort', data, name);
      if (!environment.production) {
        console.log("Saved setting", data, name);
      }
    }
    else {
      console.error("Unable to save setting");
    }
  }

  public async getSetting(name: string): Promise<string | undefined> {
    if (this.settingsDbReady) {
      await this.settingsDbReady;
    }
    if (this.settingsDb) {
      const result = await this.settingsDb.get('Settings', name);
      if (!environment.production) {
        console.log("Received setting", name, result);
      }
      return result;
    }
    return undefined;
  }

  public async saveSetting(name: string, value: string): Promise<void> {
    if (this.settingsDb) {
      await this.settingsDb.put('Settings', value, name);
      if (!environment.production) {
        console.log("Saved setting", name, value)
      }
    }
  }

  public async deleteSetting(name: string): Promise<void> {
    if (this.settingsDb) {
      await this.settingsDb.delete('Settings', name);
      if (!environment.production) {
        console.log("Deleted setting", name);
      }
    }
  }

  private updateNetworkLoading(): void {
    const networkLoading = (this.httpLoading || this.webSocketLoading);
    if (networkLoading != this.networkLoading) {
      this.networkLoading = networkLoading;
      this.networkLoadingChanged.emit(this.networkLoading);
    }
  }

  public async oAuth(code: string, state: string): Promise<OAuthResponse> {
    const response = await firstValueFrom(this.httpClient.post<OAuthResponse>(this.apiUrl + 'user/OAuth', {
      State: state,
      Code: code,
    }, {
      withCredentials: true,
    }));
    if (response.success) {
      this.webSocketService.reconnect();
      this.router.navigate(["/"]);
    }
    return response;
  }
}

interface MeResponse {
  loggedIn: boolean;
  user: User | null;
}

export interface User {
  Commander: string | null;
  JournalLastImport: string | null;
}

interface OAuthResponse {
  success: boolean;
  me: MeResponse | null;
  error: string[] | null;
}

interface TableSortSetting {
  Column: string;
  Direction: SortDirection;
}

interface SettingsDb {
  'TableSort': {
    key: string,
    value: TableSortSetting,
  };
  'Settings': {
    key: string,
    value: string,
  };
}
