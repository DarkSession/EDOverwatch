import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { AppService } from 'src/app/services/app.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';

@UntilDestroy()
@Component({
  selector: 'app-contribute-data',
  templateUrl: './contribute-data.component.html',
  styleUrls: ['./contribute-data.component.css']
})
export class ContributeDataComponent implements OnInit {
  public loginDisabled = false;

  public constructor(
    private readonly appService: AppService,
    private readonly changeDetectorRef: ChangeDetectorRef,
  ) {
  }

  public ngOnInit(): void {
    this.appService.onUserChanged
    .pipe(untilDestroyed(this))
    .subscribe(() => {
      this.updateUser();
      this.changeDetectorRef.detectChanges();
    });
    this.updateUser();
  }

  private updateUser(): void {
    if (this.appService.user && this.appService.user.HasActiveToken) {
      this.loginDisabled = true;
    }
  }

  public async oAuth(): Promise<void> {
    if (this.loginDisabled) {
      return;
    }
    this.loginDisabled = true;
    this.appService.oAuthStart();
  }
}
