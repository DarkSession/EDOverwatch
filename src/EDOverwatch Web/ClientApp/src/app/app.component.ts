import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, HostListener, OnInit } from '@angular/core';
import { SwUpdate } from '@angular/service-worker';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { AppService } from './services/app.service';
import { AuthenticationStatusAnnouncement, ServerStatus, WebsocketService } from './services/websocket.service';
import { MatSnackBar, MatSnackBarRef, TextOnlySnackBar } from '@angular/material/snack-bar';
import { MatDrawerMode } from '@angular/material/sidenav';
import { faChartNetwork, faChartSimple, faContainerStorage, faCrystalBall, faHandsHoldingDiamond, faHandshake, faSolarSystem, faSwords, faTimelineArrow, faUsers, faGears } from '@fortawesome/pro-duotone-svg-icons';
import { faHandWave, faMapLocation } from '@fortawesome/pro-light-svg-icons';
import { environment } from 'src/environments/environment';

@UntilDestroy()
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['app.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent implements OnInit, AfterViewInit {
  public readonly faTimelineArrow = faTimelineArrow;
  public readonly faSolarSystem = faSolarSystem;
  public readonly faHandshake = faHandshake;
  public readonly faChartSimple = faChartSimple;
  public readonly faMapLocation = faMapLocation;
  public readonly faCrystalBall = faCrystalBall;
  public readonly faChartNetwork = faChartNetwork;
  public readonly faUsers = faUsers;
  public readonly faHandsHoldingDiamond = faHandsHoldingDiamond;
  public readonly faHandWave = faHandWave;
  public readonly faSwords = faSwords;
  public readonly faContainerStorage = faContainerStorage;
  public readonly faGears = faGears;
  public loading = false;
  public sideNavMode: MatDrawerMode = "side";
  private serverStatusSnackbar: MatSnackBarRef<TextOnlySnackBar> | null = null;
  /*
  @HostListener('window:focus', ['$event'])
  onFocus(event: any): void {
    this.websocketService.ensureConnected();
  }
  */
  private announcement: AuthenticationStatusAnnouncement | null = null;
  public showAnnouncement = false;
  public announcementText = "";

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
    this.websocketService.siteAnnouncementReceived.subscribe(async (announcement: AuthenticationStatusAnnouncement | null) => {
      if (announcement === null) {
        await this.appService.deleteSetting("siteAnnouncement");
      }
      else {
        await this.appService.saveSetting("siteAnnouncement", JSON.stringify(announcement));
      }
      this.processSiteAnnouncement(announcement);
    });
    setInterval(() => {
      this.checkAnnouncement();
    }, 60000);
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
      this.matSnackBar.open("Overwatch was reloaded to apply the latest update.", "Dismiss", {
        duration: 6000,
      });
    }
    const siteAnnouncement = await this.appService.getSetting("siteAnnouncement");
    if (siteAnnouncement) {
      const announcement: AuthenticationStatusAnnouncement = JSON.parse(siteAnnouncement);
      this.processSiteAnnouncement(announcement);
    }
  }

  private processSiteAnnouncement(announcement: AuthenticationStatusAnnouncement | null): void {
    this.announcement = announcement;
    this.checkAnnouncement();
  }

  private checkAnnouncement(): void {
    if (!environment.production) {
      console.log("checkAnnouncement"); 
    }
    if (this.announcement === null) {
      this.showAnnouncement = false;
    }
    else {
      const now = new Date();
      const showFrom = new Date(this.announcement.ShowFrom);
      const showTo = new Date(this.announcement.ShowTo);
      this.showAnnouncement = (showFrom <= now && showTo >= now);
      this.announcementText = this.announcement.Text;
    }
    this.changeDetectorRef.markForCheck();
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
    this.websocketService.serverStatusChanged
      .pipe(untilDestroyed(this))
      .subscribe((status: ServerStatus) => {
        if (this.serverStatusSnackbar != null) {
          this.serverStatusSnackbar.dismiss();
          this.serverStatusSnackbar = null;
        }
        switch (status) {
          case ServerStatus.Down: {
            this.serverStatusSnackbar = this.matSnackBar.open("The Overwatch server is currently unreachable. Connection will be established once it is available again.");
            break;
          }
          case ServerStatus.Maintenance: {
            this.serverStatusSnackbar = this.matSnackBar.open("Overwatch is currently unavailable for planned maintaince. Connection will be established once it is available again.");
            break;
          }
        }
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

  public async requestJournalImport(): Promise<void> {
    if (!this.appService.user?.HasActiveToken) {
      return;
    }
    const response = await this.websocketService.sendMessageAndWaitForResponse<CommanderCApiImportRequestResponse>("CommanderCApiImportRequest", {});
    if (response && response.Data) {
      const responseData = response.Data;
      if (responseData.Message) {
        this.matSnackBar.open(responseData.Message, "Dismiss", {
          duration: 5000,
        });
      }
    }
  }
}

interface CommanderCApiImportRequestResponse {
  Requested: boolean;
  Message: string | null;
}