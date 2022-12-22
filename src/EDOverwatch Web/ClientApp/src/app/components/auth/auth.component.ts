import { ChangeDetectorRef, Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppService } from '../../services/app.service';

@Component({
  selector: 'app-auth',
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css']
})
export class AuthComponent {
  public errors: string[] = [];
  public loading = true;

  public constructor(
    private readonly route: ActivatedRoute,
    private readonly appService: AppService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) { }

  public ngOnInit(): void {
    const code = this.route.snapshot.queryParams["code"];
    const state = this.route.snapshot.queryParams["state"];
    if (code && state) {
      this.authenticate(code, state);
    }
  }

  private async authenticate(code: string, state: string): Promise<void> {
    const response = await this.appService.oAuth(code, state);
    if (response.error) {
      this.errors = response.error;
    }
    this.loading = false;
    this.changeDetectorRef.detectChanges();
  }
}
