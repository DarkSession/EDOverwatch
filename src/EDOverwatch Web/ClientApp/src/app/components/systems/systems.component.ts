import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchStarSystem, SystemListComponent } from '../system-list/system-list.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';
import { AppService } from 'src/app/services/app.service';
import { faArrowUpRightFromSquare, faCircleXmark, faFilter } from '@fortawesome/free-solid-svg-icons';
import { OnDestroy } from '@angular/core';
import { ChangeDetectionStrategy } from '@angular/core';

@UntilDestroy()
@Component({
  selector: 'app-systems',
  templateUrl: './systems.component.html',
  styleUrls: ['./systems.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemsComponent implements OnInit, OnDestroy {
  public readonly faArrowUpRightFromSquare = faArrowUpRightFromSquare;
  public readonly faFilter = faFilter;
  public readonly faCircleXmark = faCircleXmark;
  public dataRaw: OverwatchStarSystem[] = [];
  public hideUnpopulated: boolean = false;
  public hideCompleted: boolean = false;
  @ViewChild(SystemListComponent) systemList: SystemListComponent | null = null;
  public systemNameFilter: string = "";
  public optionalColumns: string[] = [];
  public nextTick: string | null = null;
  private refreshTimer: any | null = null;

  public availableOptionalColumns: {
    key: string;
    value: string;
  }[] = [
      {
        key: "EffortFocus",
        value: "Focus",
      }, {
        key: "DistanceToMaelstrom",
        value: "Distance to maelstrom",
      },
      {
        key: "PopulationOriginal",
        value: "Population (Original)",
      },
      {
        key: "ProgressReportedCompletion",
        value: "Reported progress completion",
      },
    ];

  public maelstroms: OverwatchMaelstrom[] = [];
  public maelstromsSelected: OverwatchMaelstrom[] = [];

  public thargoidLevels: OverwatchThargoidLevel[] = [];
  public thargoidLevelsSelected: OverwatchThargoidLevel[] = [];

  public constructor(
    private readonly appService: AppService,
    private readonly webSocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.webSocketService
      .on<OverwatchSystems>("OverwatchSystems")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.update(message.Data);
      });
    this.webSocketService
      .onReady
      .pipe(untilDestroyed(this))
      .subscribe((isReconnect: boolean) => {
        if (isReconnect) {
          this.webSocketService.sendMessage("OverwatchSystems", {});
        }
      });
    this.refreshTimer = setInterval(() => {
      this.changeDetectorRef.markForCheck();
    }, 1000);
    this.webSocketService.sendMessage("OverwatchSystems", {});
  }

  public ngOnDestroy(): void {
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  private async update(data: OverwatchSystems) {
    this.nextTick = data.NextTick;
    this.dataRaw = data.Systems;
    this.maelstroms = data.Maelstroms.sort((a, b) => (a.Name > b.Name) ? 1 : -1)
    const maelstromsSelectedSetting = await this.appService.getSetting("Maelstroms");
    if (maelstromsSelectedSetting) {
      const maelstromsSelected = maelstromsSelectedSetting.split(",");
      this.maelstromsSelected = maelstromsSelected.map(maelstromName => this.maelstroms.find(m => m.Name === maelstromName) as OverwatchMaelstrom);
    }
    else {
      this.maelstromsSelected = data.Maelstroms;
    }

    this.thargoidLevels = data.Levels;
    const thargoidLevelsSetting = await this.appService.getSetting("ThargoidLevels");
    if (thargoidLevelsSetting) {
      const thargoidLevelsSelected = thargoidLevelsSetting.split(",");
      this.thargoidLevelsSelected = thargoidLevelsSelected.map(thargoidLevel => this.thargoidLevels.find(t => t.Name === thargoidLevel) as OverwatchThargoidLevel);
    }
    else {
      this.thargoidLevelsSelected = data.Levels;
    }
    this.hideUnpopulated = (await this.appService.getSetting("SystemListHideUnpopulated")) == "1";
    this.hideCompleted = (await this.appService.getSetting("SystemListHideCompleted")) == "1";
    const optionalColumnsSetting = await this.appService.getSetting("SystemListOptionalColumns");
    if (optionalColumnsSetting) {
      this.optionalColumns = optionalColumnsSetting.split(",");
    }
    this.changeDetectorRef.markForCheck();
  }

  public async settingChanged(): Promise<void> {
    if (this.maelstromsSelected) {
      if (this.maelstromsSelected.length === this.maelstroms.length) {
        await this.appService.deleteSetting("Maelstroms");
      }
      else {
        await this.appService.saveSetting("Maelstroms", this.maelstromsSelected.map(m => m.Name).join(","));
      }
    }
    if (this.thargoidLevelsSelected) {
      if (this.thargoidLevelsSelected.length === this.thargoidLevels.length) {
        await this.appService.deleteSetting("ThargoidLevels");
      }
      else {
        await this.appService.saveSetting("ThargoidLevels", this.thargoidLevelsSelected.map(m => m.Name).join(","));
      }
    }
    if (this.optionalColumns && this.optionalColumns.length > 0) {
      await this.appService.saveSetting("SystemListOptionalColumns", this.optionalColumns.join(","));
    }
    else {
      this.appService.deleteSetting("SystemListOptionalColumns");
    }
    await this.appService.saveSetting("SystemListHideUnpopulated", this.hideUnpopulated ? "1" : "0");
    await this.appService.saveSetting("SystemListHideCompleted", this.hideCompleted ? "1" : "0");
    this.updateDataSource();
  }

  public updateDataSource(): void {
    if (this.systemList) {
      this.systemList.updateDataSource();
    }
  }

  public async resetFilter(): Promise<void> {
    this.hideUnpopulated = false;
    this.hideCompleted = false;
    this.maelstromsSelected = this.maelstroms;
    this.thargoidLevelsSelected = this.thargoidLevels;
    if (this.systemList) {
      this.systemList.updateDataSource();
    }
    await this.appService.deleteSetting("Maelstroms");
    await this.appService.deleteSetting("ThargoidLevels");
    await this.appService.deleteSetting("SystemListHideUnpopulated");
    await this.appService.deleteSetting("SystemListHideCompleted");
  }
}

export interface OverwatchSystems {
  Maelstroms: OverwatchMaelstrom[];
  Levels: OverwatchThargoidLevel[];
  Systems: OverwatchStarSystem[];
  NextTick: string;
}

