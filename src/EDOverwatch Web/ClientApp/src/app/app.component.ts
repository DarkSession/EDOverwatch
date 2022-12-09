import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { delay } from 'rxjs';
import { AppService } from './app.service';
import { HttpInterceptorService } from './http-interceptor.service';

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
    public readonly appService: AppService,
    private readonly httpInterceptorService: HttpInterceptorService
  ) {
  }

  public ngOnInit(): void {
    this.httpInterceptorService.loadingSub
      .pipe(delay(0)) // This prevents a ExpressionChangedAfterItHasBeenCheckedError for subsequent requests
      .pipe(untilDestroyed(this))
      .subscribe((loading) => {
        this.loading = loading;
      });
    this.appService.onUserChanged
      .pipe(untilDestroyed(this))
      .subscribe(() => {
        this.changeDetectorRef.detectChanges();
      });

  }

  public toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
    this.changeDetectorRef.detectChanges();
  }
}
