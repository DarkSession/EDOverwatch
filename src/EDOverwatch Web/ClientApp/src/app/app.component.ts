import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { AppService } from './services/app.service';

@UntilDestroy()
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['app.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent implements OnInit {
  public isMenuOpen = false;
  public loading = false;

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    public readonly appService: AppService) {
  }

  public ngOnInit(): void {
    this.appService.onUserChanged
      .pipe(untilDestroyed(this))
      .subscribe(() => {
        this.changeDetectorRef.markForCheck();
      });
    this.appService.networkLoadingChanged
      .pipe(untilDestroyed(this))
      .subscribe((loading: boolean) => {
        this.loading = loading;
        this.changeDetectorRef.markForCheck();
      });
    if (localStorage.getItem("menuOpen") === "1") {
      this.isMenuOpen = true;
    }
  }

  public toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
    localStorage.setItem("menuOpen", this.isMenuOpen ? "1" : "0");
    this.changeDetectorRef.markForCheck();
  }
}
