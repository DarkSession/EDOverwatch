import { ChangeDetectorRef, Component, SecurityContext } from '@angular/core';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';

@UntilDestroy()
@Component({
  selector: 'app-map-historical',
  templateUrl: './map-historical.component.html',
  styleUrls: ['./map-historical.component.css']
})
export class MapHistoricalComponent {
  public urlBase = "https://darksession.github.io/CanonnED3D-Map-DCoH/dcoh_historical.html?date=";
  public url: SafeUrl = "";
  public date: string = "";
  public thargoidCycles: OverwatchThargoidCycle[] = [];

  public constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly websocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly sanitizer: DomSanitizer,
  ) {
    this.url = this.sanitizer.bypassSecurityTrustResourceUrl(this.urlBase);
  }

  public ngOnInit(): void {
    this.route.paramMap
      .pipe(untilDestroyed(this))
      .subscribe((p: ParamMap) => {
        const date = p.get("date") ?? "";
        if (date) {
          this.date = date;
          this.url = this.sanitizer.bypassSecurityTrustResourceUrl(this.urlBase + date);
          this.changeDetectorRef.detectChanges();
        }
      });
    this.websocketService.on<OverwatchThargoidCycle[]>("OverwatchThargoidCycles")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message && message.Data) {
          this.thargoidCycles = message.Data;
        }
        this.changeDetectorRef.detectChanges();
      });
    this.websocketService.sendMessage("OverwatchThargoidCycles", {});
  }

  public dateChanged(): void {
    this.router.navigate(["map", this.date]);
  }
}

interface OverwatchThargoidCycle {
  Cycle: string;
  Start: string;
  End: string;
  IsCurrent: boolean;
}