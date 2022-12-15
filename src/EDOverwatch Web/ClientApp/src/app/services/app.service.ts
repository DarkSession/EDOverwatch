import { HttpClient } from '@angular/common/http';
import { EventEmitter, Inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { HttpInterceptorService } from './http-interceptor.service';
import { ConnectionStatus, WebSocketMessage, WebsocketService } from './websocket.service';

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

  constructor(
    private readonly httpClient: HttpClient,
    @Inject('API_URL') private readonly apiUrl: string,
    private readonly router: Router,
    private readonly webSocketService: WebsocketService,
    private readonly httpInterceptorService: HttpInterceptorService) {
    this.webSocketService.onConnectionStatusChanged.subscribe((connectionStatus: ConnectionStatus) => {
      if (connectionStatus === ConnectionStatus.Connected && this.webSocketService.connectionIsAuthenticated) {
        this.requestUser();
      }
      else if (connectionStatus === ConnectionStatus.Disconnected) {
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
  }

  private updateNetworkLoading(): void {
    const networkLoading = (this.httpLoading || this.webSocketLoading);
    if (networkLoading != this.networkLoading) {
      this.networkLoading = networkLoading;
      this.networkLoadingChanged.emit(this.networkLoading);
    }
  }

  private async requestUser(): Promise<void> {
    const response = await this.webSocketService.sendMessageAndWaitForResponse<User>("CommanderMe", {});
    if (response) {
      this.user = response.Data;
      this.onUserChanged.emit();
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