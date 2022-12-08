import { HttpClient } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AppService } from '../app.service';

@Component({
  selector: 'app-auth',
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css']
})
export class AuthComponent {
  public errors: string[] = [];

  public constructor(
    private readonly httpClient: HttpClient,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly appService: AppService,
    @Inject('BASE_URL') private readonly baseUrl: string
  ) { }

  public ngOnInit(): void {
    const code = this.route.snapshot.queryParams["code"];
    const state = this.route.snapshot.queryParams["state"];
    if (code && state) {
      this.authenticate(code, state);
    }
  }

  private async authenticate(code: string, state: string): Promise<void> {
    const response = await firstValueFrom(this.httpClient.post<OAuthResponse>(this.baseUrl + 'api/user/OAuth', {
      State: state,
      Code: code,
    }, {
      withCredentials: true,
    }));
    if (response.success) {
      await this.appService.getUser();
      this.router.navigate(["/"]);
    }
    else {
      this.errors = response.error ?? [];
    }
  }
}

interface OAuthResponse {
  success: boolean; 
  error: string[] | null;
}