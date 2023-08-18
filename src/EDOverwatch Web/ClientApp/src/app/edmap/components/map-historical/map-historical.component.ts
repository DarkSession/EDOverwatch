import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { ED3DMap } from 'canonned3d-map/lib/ED3DMap';
import { WebsocketService } from 'src/app/services/websocket.service';
import { ammoniaWorlds } from '../../data/ammonia-worlds';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { solSite } from '../../data/sol';
import { AppService } from 'src/app/services/app.service';
import { SystemConfiguration } from 'canonned3d-map/lib/System';
import { barnacleBarbs } from '../../data/barnacle-barbs';
import { abandonedBases } from '../../data/abandoned-bases';
import { thargoidSites } from '../../data/thargoid-sites';
import { unknownBarnacleSites } from '../../data/unknown-barnacle-sites';
import { horizonEngineers, odysseyEngineers } from '../../data/engineers';
import { faAngleLeft, faAngleRight } from '@fortawesome/free-solid-svg-icons';
import { OverwatchStarSystemsHistorical } from 'src/app/components/systems-historical-cycle/systems-historical-cycle.component';
import { OverwatchThargoidCycle } from 'src/app/components/home-v2/home-v2.component';

@UntilDestroy()
@Component({
  selector: 'app-map-historical',
  templateUrl: './map-historical.component.html',
  styleUrls: ['./map-historical.component.scss', '../map.scss'],
  encapsulation: ViewEncapsulation.ShadowDom,
})
export class MapHistoricalComponent implements OnInit, AfterViewInit {
  public readonly faAngleRight = faAngleRight;
  public readonly faAngleLeft = faAngleLeft;
  @ViewChild('container', { static: true }) container?: ElementRef;
  private ed3dMap: ED3DMap | null = null;
  public thargoidCycles: OverwatchThargoidCycle[] = [];
  private readonly categories = {
    "Systems": {
      "00": {
        name: "Sol",
        color: "78b7f6",
      },
    },
    "States": {
      "Clear": {
        name: "Clear",
        color: "333333"
      },
      "ClearNew": {
        name: "Clear (New)",
        color: "ffffff"
      },
      "AlertNew": {
        name: "Alert (New)",
        color: "f1c232"
      },
      "Invasion": {
        name: "Invasion",
        color: "993000"
      },
      "InvasionNew": {
        name: "Invasion (New)",
        color: "ff7433"
      },
      "Controlled": {
        name: "Controlled",
        color: "13290a"
      },
      "ControlledNew": {
        name: "Controlled (New)",
        color: "80d75b"
      },
      "Titan": {
        name: "Titan",
        color: "4d0000"
      },
      "Recovery": {
        name: "Recovery",
        color: "590099"
      },
      "RecoveryNew": {
        name: "Recovery (New)",
        color: "aa33ff"
      },
    },
    "Thargoid POI": {
      "Ammonia": {
        name: 'Nearby ammonia worlds (< 30 Ly)',
        color: "4e290a",
      },
      "Thargoid Structure": {
        name: "Thargoid Structure",
        color: "512B60"
      },
      "Barnacle Barbs": {
        name: "Barnacle Barbs",
        color: "0B5345"
      },
      "Unknown Site": {
        name: "Unknown Site",
        color: "2E4840"
      },
    },
    "Various POI": {
      "Abandoned Base": {
        name: "Abandoned Base",
        color: "283747",
      },
      "Horizons Engineer": {
        name: "Engineer (Horizons)",
        color: "206694",
      },
      "Odyssey Engineer": {
        name: "Engineer (Odyssey)",
        color: "3498db",
      },
    },
  };
  private defaultActiveCategories = ["ClearNew", "AlertNew", "Invasion", "InvasionNew", "Controlled", "ControlledNew", "Titan", "Recovery", "RecoveryNew"];
  public date: string = "";
  public dateSelectionDisabled = true;
  private paraLoaded = false;

  public constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly websocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly appService: AppService
  ) {
  }

  public ngOnInit(): void {
    this.websocketService.on<OverwatchThargoidCycle[]>("OverwatchThargoidCycles")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message && message.Data) {
          this.thargoidCycles = message.Data;
          if (this.paraLoaded && !this.date) {
            const thargoidCycle = this.thargoidCycles.find(t => t.IsCurrent);
            if (thargoidCycle) {
              this.date = thargoidCycle.Cycle;
              this.dateChanged();
            }
          }
        }
        this.changeDetectorRef.detectChanges();
      });
    this.appService.isMenuOpenChanged
      .pipe(untilDestroyed(this))
      .subscribe(() => {
        setTimeout(() => {
          if (this.ed3dMap) {
            this.ed3dMap.windowResize();
          }
        }, 500);
      });
    this.websocketService.sendMessage("OverwatchThargoidCycles", {});
  }

  public dateChanged(): void {
    if (this.date) {
      this.router.navigate(['/', 'edmap', 'historical', this.date]);
    }
  }

  private async requestCycleData(cycle: string): Promise<void> {
    this.date = cycle;
    this.dateSelectionDisabled = true;
    this.changeDetectorRef.detectChanges();
    const response = await this.websocketService.sendMessageAndWaitForResponse<OverwatchStarSystemsHistorical>("OverwatchSystemsHistoricalCycle", {
      Cycle: cycle,
    });
    if (response && response.Data && this.ed3dMap) {
      const systems: SystemConfiguration[] = [solSite];
      for (const data of response.Data.Systems) {
        const description =
          `<b>State</b>: ${data.ThargoidLevel.Name}<br>` +
          `<b>Previous state</b>: ${data.PreviousThargoidLevel.Name}<br>` +
          `<b>Titan</b>: ${data.Maelstrom.Name}<br>`;
        const poiSite = {
          name: data.Name,
          description: description,
          categories: [data.State],
          coordinates: { x: data.Coordinates.X, y: data.Coordinates.Y, z: data.Coordinates.Z },
        }
        systems.push(poiSite);
      }
      for (const ammoniaWorld of ammoniaWorlds) {
        if (response.Data.Systems.some(s => Math.sqrt(Math.pow(Math.abs(s.Coordinates.X - ammoniaWorld.coordinates.x), 2) + Math.pow(Math.abs(s.Coordinates.Y - ammoniaWorld.coordinates.y), 2) + Math.pow(Math.abs(s.Coordinates.Z - ammoniaWorld.coordinates.z), 2)) <= 30)) {
          systems.push(ammoniaWorld);
        }
      }

      systems.push(...barnacleBarbs);
      systems.push(...abandonedBases);
      systems.push(...thargoidSites);
      systems.push(...unknownBarnacleSites);
      systems.push(...horizonEngineers);
      systems.push(...odysseyEngineers);

      await this.ed3dMap.updateSystems(systems, this.categories);
    }
    this.dateSelectionDisabled = false;
    this.changeDetectorRef.detectChanges();
  }

  public async ngAfterViewInit(): Promise<void> {
    if (this.container) {
      this.ed3dMap = new ED3DMap(this.container.nativeElement, {
        showGalaxyInfos: true,
        startAnimation: true,
        systems: [solSite],
        categories: this.categories,
        activeCategories: this.defaultActiveCategories,
        withOptionsPanel: true,
        withHudPanel: true,
        showSystemSearch: true,
        hideFilteredSystems: true,
        systemCategoryFilterMatchAny: true,
      });
      await this.ed3dMap.start();

      this.route.paramMap
        .pipe(untilDestroyed(this))
        .subscribe((p: ParamMap) => {
          let date = p.get("date");
          this.paraLoaded = true;
          if (!date) {
            const thargoidCycle = this.thargoidCycles.find(t => t.IsCurrent);
            if (thargoidCycle) {
              this.date = thargoidCycle.Cycle;
              this.dateChanged();
            }
            return;
          }
          this.requestCycleData(date);
        });
    }
  }

  public previousCycle(): void {
    const currentIndex = this.thargoidCycles.findIndex(t => t.Cycle === this.date);
    if (currentIndex > 0) {
      this.date = this.thargoidCycles[(currentIndex - 1)].Cycle;
      this.dateChanged();
    }
  }

  public nextCycle(): void {
    const currentIndex = this.thargoidCycles.findIndex(t => t.Cycle === this.date);
    if (currentIndex !== -1) {
      const index = currentIndex + 1;
      if (index < this.thargoidCycles.length) {
        this.date = this.thargoidCycles[index].Cycle;
        this.dateChanged();
      }
    }
  }
}

