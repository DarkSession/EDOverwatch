import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, HostListener, OnInit } from '@angular/core';
import { SwUpdate } from '@angular/service-worker';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { AppService } from './services/app.service';
import { WebsocketService } from './services/websocket.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDrawerMode } from '@angular/material/sidenav';

@UntilDestroy()
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['app.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent implements OnInit, AfterViewInit {
  public loading = false;
  public sideNavMode: MatDrawerMode = "side";
  /*
  @HostListener('window:focus', ['$event'])
  onFocus(event: any): void {
    this.websocketService.ensureConnected();
  }
  */

  @HostListener('window:resize', ['$event'])
  onFocus(event: any): void {
    this.updateSideNavMode();
  }

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    public readonly appService: AppService,
    private readonly swUpdate: SwUpdate,
    private readonly websocketService: WebsocketService,
    private readonly matSnackBar: MatSnackBar
  ) {
  }

  private updateSideNavMode() {
    const newValue: MatDrawerMode = window.innerWidth < 1000 ? "over" : "side";
    this.sideNavMode = newValue;
  }

  public ngAfterViewInit(): void {
    this.checkAppUpdated();
  }

  private async checkAppUpdated(): Promise<void> {
    const appUpdated = await this.appService.getSetting("appUpdated");
    if (appUpdated === "1") {
      await this.appService.deleteSetting("appUpdated");
      this.matSnackBar.open("Overwatch has been updated!", "Dismiss", {
        duration: 6000,
      });
    }
  }

  public menuOpenChanged(isOpen: boolean) {
    if (isOpen != this.appService.isMenuOpen) {
      this.appService.toggleIsMenuOpen();
    }
  }

  public ngOnInit(): void {
    this.updateSideNavMode();
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
    if (this.swUpdate) {
      this.swUpdate.versionUpdates
        .pipe(untilDestroyed(this))
        .subscribe(evt => {
          switch (evt.type) {
            case 'VERSION_DETECTED': {
              console.log(`Downloading new app version: ${evt.version.hash}`);
              break;
            }
            case 'VERSION_READY': {
              console.log(`New app version ready for use: ${evt.latestVersion.hash}`);
              this.swUpdate.activateUpdate().then(async () => {
                await this.appService.saveSetting("appUpdated", "1");
                document.location.reload();
              });
              break;
            }
            case 'VERSION_INSTALLATION_FAILED': {
              console.error(`Failed to install app version '${evt.version.hash}': ${evt.error}`);
              break;
            }
          }
        });
    }
  }

  public toggleMenu(): void {
    this.appService.toggleIsMenuOpen();
    this.changeDetectorRef.markForCheck();
  }
}
