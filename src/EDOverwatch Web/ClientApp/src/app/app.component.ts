import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { delay } from 'rxjs';
import { AppService } from './services/app.service';
import { HttpInterceptorService } from './services/http-interceptor.service';
import { WebsocketService } from './services/websocket.service';

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
    private readonly httpInterceptorService: HttpInterceptorService,
    private readonly websocketService: WebsocketService
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
    if (localStorage.getItem("menuOpen") === "1") {
      this.isMenuOpen = true;
    }
  }

  public toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
    localStorage.setItem("menuOpen", this.isMenuOpen ? "1" : "0");
    this.changeDetectorRef.detectChanges();
  }
}
