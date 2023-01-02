import { Component } from '@angular/core';
import { AppService } from 'src/app/services/app.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  public loginDisabled = false;

  public constructor(private readonly appService: AppService) {
  }

  public async oAuth(): Promise<void> {
    if (this.loginDisabled) {
      return;
    }
    this.loginDisabled = true;
    this.appService.oAuthStart();
  }
}

