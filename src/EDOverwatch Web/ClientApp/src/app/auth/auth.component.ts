import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppService } from '../app.service';

@Component({
  selector: 'app-auth',
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css']
})
export class AuthComponent {
  public errors: string[] = [];

  public constructor(
    private readonly route: ActivatedRoute,
    private readonly appService: AppService
  ) { }

  public ngOnInit(): void {
    const code = this.route.snapshot.queryParams["code"];
    const state = this.route.snapshot.queryParams["state"];
    if (code && state) {
      this.authenticate(code, state);
    }
  }

  private async authenticate(code: string, state: string): Promise<void> {
    await this.appService.oAuth(code, state);
  }
}
