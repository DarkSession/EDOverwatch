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
    @Inject('BASE_URL') private readonly baseUrl: string) {
  }

  public async oAuth(): Promise<void> {
    if (this.loginDisabled) {
      return;
    }
    this.loginDisabled = true;
    try {
      const response = await firstValueFrom(this.httpClient.post<OAuthGetStateResponse>(this.baseUrl + 'api/user/OAuthGetUrl', {}, {
        withCredentials: true,
      }));
      if (response) {
        console.log(response);
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