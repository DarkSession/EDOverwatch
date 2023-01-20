import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { ED3DMap } from 'canonned3d-map/lib/ED3DMap';
import { OverwatchSystems } from 'src/app/components/systems/systems.component';
import { WebsocketService } from 'src/app/services/websocket.service';
import { solSite } from '../../data/sol';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchStarSystem } from 'src/app/components/system-list/system-list.component';
import { ammoniaWorlds } from '../../data/ammonia-worlds';
import { DateAgoPipe } from 'src/app/pipes/date-ago.pipe';
import { AppService } from 'src/app/services/app.service';

@UntilDestroy()
@Component({
  selector: 'app-current',
  templateUrl: './current.component.html',
  styleUrls: ['./current.component.css', '../map.scss'],
  encapsulation: ViewEncapsulation.ShadowDom,
})
export class CurrentComponent implements OnInit, AfterViewInit {
  @ViewChild('container', { static: true }) container?: ElementRef;
  private ed3dMap: ED3DMap | null = null;
  private mapLoaded = false;
  private readonly categories = {
    "Systems": {
      "00": {
        name: "Sol",
        color: "78b7f6",
      },
    },
    "States": {
      "20": {
        name: "Alert",
        color: "f1c232"
      },
      "30": {
        name: "Invasion",
        color: "ff5200"
      },
      "40": {
        name: "Controlled",
        color: "38761d"
      },
      "50": {
        name: "Maelstrom",
        color: "cc0000"
      },
      "70": {
        name: "Recovery",
        color: "9f1bff"
      }
    },
    "Thargoid POI": {
      "Ammonia": {
        name: 'Nearby ammonia worlds (< 30 Ly)',
        color: "4e290a",
      }
    },
  }
  private readonly defaultActiveCategories = ["20", "30", "40", "50", "70"];
  private systems: OverwatchStarSystem[] = [];

  public constructor(
    private readonly websocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly appService: AppService
  ) {
  }

  public ngOnInit(): void {
    this.websocketService
      .on<OverwatchSystems>("OverwatchSystems")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message.Data && message.Data.Systems) {
          this.systems = message.Data.Systems;
          this.update();
        }
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
    this.websocketService.sendMessage("OverwatchSystems", {});;
  }

  private async update() {
    if (this.ed3dMap && this.mapLoaded && this.systems.length) {
      const dateAgoPipe = new DateAgoPipe();
      const systems = [solSite];
      for (const data of this.systems) {
        let description =
          `<b>State</b>: ${data.ThargoidLevel.Name}<br>` +
          `<b>Maelstrom</b>: ${data.Maelstrom.Name}<br>`;
        if (data.Progress) {
          description += `<b>Progress</b>: ${data.Progress} %<br>`;
        }
        if (data.ThargoidLevel.Level === 30) {
          description +=
            `<b>Stations Under Attack</b>: ${data.StationsUnderAttack}<br>` +
            `<b>Stations Damaged</b>: ${data.StationsDamaged}<br>`;
        }
        else if (data.ThargoidLevel.Level === 70) {
          description += `<b>Stations Under Repair</b>: ${data.StationsUnderRepair}<br>`;
        }

        if (data.StateExpiration?.StateExpires) {
          description += `<b>Time remaining</b>: ${dateAgoPipe.transform(data.StateExpiration.StateExpires)}<br>`;
        }
        const poiSite = {
          name: data.Name,
          description: description,
          categories: [data.ThargoidLevel.Level.toString()],
          coordinates: { x: data.Coordinates.X, y: data.Coordinates.Y, z: data.Coordinates.Z },
        }
        systems.push(poiSite);
      }
      for (const ammoniaWorld of ammoniaWorlds) {
        if (this.systems.some(s => Math.sqrt(Math.pow(Math.abs(s.Coordinates.X - ammoniaWorld.coordinates.x), 2) + Math.pow(Math.abs(s.Coordinates.Y - ammoniaWorld.coordinates.y), 2) + Math.pow(Math.abs(s.Coordinates.Z - ammoniaWorld.coordinates.z), 2)) <= 30)) {
          systems.push(ammoniaWorld);
        }
      }
      await this.ed3dMap.updateSystems(systems, this.categories);
    }
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
      this.mapLoaded = true;
      this.update();
    }
  }
}
