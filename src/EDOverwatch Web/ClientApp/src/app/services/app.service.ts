import { HttpClient } from '@angular/common/http';
import { EventEmitter, Inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AppService {
  public isLoggedIn = false;
  public user: User | null = null;
  public onUserChanged: EventEmitter<void> = new EventEmitter<void>();

  constructor(
    private readonly httpClient: HttpClient,
    @Inject('API_URL') private readonly apiUrl: string,
    private readonly router: Router) {
  }

  public async oAuth(code: string, state: string): Promise<OAuthResponse> {
    const response = await firstValueFrom(this.httpClient.post<OAuthResponse>(this.apiUrl + 'user/OAuth', {
      State: state,
      Code: code,
    }, {
      withCredentials: true,
    }));
    if (response.success) {
      this.isLoggedIn = true;
      this.user = response.me!.user;
      this.onUserChanged.emit();
      this.router.navigate(["/"]);
    }
    else {
      this.isLoggedIn = false;
    }
    return response;
  }
}

interface MeResponse {
  loggedIn: boolean;
  user: User | null;
}

export interface User {
  commander: string | null;
  journalLastImport: string | null;
}

interface OAuthResponse {
  success: boolean;
  me: MeResponse | null;
  error: string[] | null;
}