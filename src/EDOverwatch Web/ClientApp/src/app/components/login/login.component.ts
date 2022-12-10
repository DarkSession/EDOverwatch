import { HttpClient } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  public loginDisabled = false;

  public constructor(
    private readonly httpClient: HttpClient,
    @Inject('API_URL') private readonly apiUrl: string) {
  }

  public async oAuth(): Promise<void> {
    if (this.loginDisabled) {
      return;
    }
    this.loginDisabled = true;
    try {
      const response = await firstValueFrom(this.httpClient.post<OAuthGetStateResponse>(this.apiUrl + 'user/OAuthGetUrl', {}, {
        withCredentials: true,
      }));
      if (response) {
        window.location.href = response.url;
      }
    }
    catch (e) {
      console.error(e);
    }
  }
}

interface OAuthGetStateResponse {
  url: string;
}