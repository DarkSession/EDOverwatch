import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { delay } from 'rxjs';
import { AppService } from './app.service';

@UntilDestroy()
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['app.component.scss']
})
export class AppComponent implements OnInit {
  public isMenuOpen = false;
  public loading = false;

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly appService: AppService) {
  }

  public ngOnInit(): void {
    this.appService.loadingSub
      .pipe(delay(0)) // This prevents a ExpressionChangedAfterItHasBeenCheckedError for subsequent requests
      .pipe(untilDestroyed(this))
      .subscribe((loading) => {
        this.loading = loading;
      });
  }

  public toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
    this.changeDetectorRef.detectChanges();
  }
}
