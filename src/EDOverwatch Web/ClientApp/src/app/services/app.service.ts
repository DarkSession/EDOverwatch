import { HttpClient } from '@angular/common/http';
import { EventEmitter, Inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConnectionStatus, WebsocketService } from './websocket.service';

@Injectable({
  providedIn: 'root'
})
export class AppService {
  public user: User | null = null;
  public onUserChanged: EventEmitter<void> = new EventEmitter<void>();

  constructor(
    private readonly httpClient: HttpClient,
    @Inject('API_URL') private readonly apiUrl: string,
    private readonly router: Router,
    private readonly webSocketService: WebsocketService) {
    this.webSocketService.onConnectionStatusChanged.subscribe((connectionStatus: ConnectionStatus) => {
      if (connectionStatus === ConnectionStatus.Connected && this.webSocketService.connectionIsAuthenticated) {
        this.requestUser();
      }
      else if (this.user) {
        this.user = null;
        this.onUserChanged.emit();
      }
    });
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