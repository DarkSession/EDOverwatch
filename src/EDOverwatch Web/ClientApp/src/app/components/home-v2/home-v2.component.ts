import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchOverviewV2Cycle } from '../home-v2-cycle/home-v2-cycle.component';
import { OverwatchOverviewV2CycleChange } from '../home-v2-cycle-changes/home-v2-cycle-changes.component';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';
import { OverwatchStarSystemFull, SystemListComponent } from '../system-list/system-list.component';
import { AppService } from 'src/app/services/app.service';
import { faFilters, faCircleXmark, faGears } from '@fortawesome/pro-duotone-svg-icons';
import { faBolt } from '@fortawesome/free-solid-svg-icons';
import { faCrosshairs, faCrosshairsSimple } from '@fortawesome/pro-light-svg-icons';

@UntilDestroy()
@Component({
  selector: 'app-home-v2',
  templateUrl: './home-v2.component.html',
  styleUrls: ['./home-v2.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeV2Component implements OnInit, AfterViewInit {
  public readonly faFilters = faFilters;
  public readonly faCircleXmark = faCircleXmark;
  public readonly OverviewDataStatus = OverviewDataStatus;
  public readonly faGears = faGears;
  public readonly faBolt = faBolt;
  public readonly faCrosshairs = faCrosshairs;
  public readonly faCrosshairsSimple = faCrosshairsSimple;
  @ViewChild(SystemListComponent) systemList: SystemListComponent | null = null;
  public hideUnpopulated: boolean = false;
  public hideCompleted: boolean = false;
  public systemNameFilter: string = "";
  public optionalColumns: string[] = [];
  public overview: OverwatchOverviewV2 | null = null;
  public nextTick: string | null = null;
  public maelstroms: OverwatchMaelstrom[] = [];
  public maelstromsSelected: OverwatchMaelstrom[] = [];
  public thargoidLevels: OverwatchThargoidLevel[] = [];
  public thargoidLevelsSelected: OverwatchThargoidLevel[] = [];
  public availableOptionalColumns: {
    key: string;
    value: string;
  }[] = [
      {
        key: "EffortFocus",
        value: "Focus",
      }, {
        key: "DistanceToMaelstrom",
        value: "Distance to Titan",
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
  public features: {
    key: string;
    value: string;
  }[] = [
      {
        key: "None",
        value: "None",
      },
      {
        key: "AXConflictZones",
        value: "AX conflict zones",
      },
      {
        key: "ThargoidControlledReactivationMissions",
        value: "AX reactivation missions available",
      },
      {
        key: "BarnacleMatrix",
        value: "Barnacle matrix present",
      },
      {
        key: "GroundPortAXCZ",
        value: "Ground port under attack",
      },
      {
        key: "FederalFaction",
        value: "Federal faction(s) present",
      },
      {
        key: "ImperialFaction",
        value: "Imperial faction(s) present",
      },
      {
        key: "OdysseySettlements",
        value: "Odyssey settlement(s)",
      }];
  public featuresSelected: string[] = this.features.map(f => f.key);
  public dataRaw: OverwatchStarSystemFull[] = [];
  @ViewChild("stateContainers") stateContainers: ElementRef | null = null;

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly webSocketService: WebsocketService,
    private readonly appService: AppService
  ) {
  }

  public ngOnInit(): void {
    this.webSocketService
      .on<OverwatchOverviewV2>("OverwatchHomeV2")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.update(message.Data);
      });
    this.webSocketService
      .onReady
      .pipe(untilDestroyed(this))
      .subscribe((isReconnect: boolean) => {
        if (isReconnect) {
          this.webSocketService.sendMessage("OverwatchHomeV2", {});
        }
      });
    this.webSocketService.sendMessage("OverwatchHomeV2", {});
  }

  public ngAfterViewInit(): void {
    this.updateContainer();
  }

  private updateContainer(): void {
    if (this.stateContainers && this.stateContainers.nativeElement instanceof HTMLElement) {
      const rect = this.stateContainers.nativeElement.getBoundingClientRect();
      if (rect.width < this.stateContainers.nativeElement.scrollWidth) {
        const scrollToPos = (this.stateContainers.nativeElement.scrollWidth / 2) - (rect.width / 2);
        this.stateContainers.nativeElement.scrollTo({
          left: scrollToPos,
          behavior: "instant",
        });
      }
    }
  }

  private async update(data: OverwatchOverviewV2) {
    this.nextTick = data.NextTick;
    this.dataRaw = data.Systems;
    this.overview = data;
    this.maelstroms = data.Maelstroms.sort((a, b) => (a.Name > b.Name) ? 1 : -1)
    const maelstromsSelectedSetting = await this.appService.getSetting("Maelstroms");
    if (maelstromsSelectedSetting) {
      const maelstromsSelected = maelstromsSelectedSetting.split(",");
      this.maelstromsSelected = maelstromsSelected.map(maelstromName => this.maelstroms.find(m => m.Name === maelstromName) as OverwatchMaelstrom);
    }
    else {
      this.maelstromsSelected = [...data.Maelstroms];
    }

    this.thargoidLevels = data.Levels;
    const thargoidLevelsSetting = await this.appService.getSetting("ThargoidLevels");
    if (thargoidLevelsSetting) {
      const thargoidLevelsSelected = thargoidLevelsSetting.split(",");
      this.thargoidLevelsSelected = thargoidLevelsSelected.map(thargoidLevel => this.thargoidLevels.find(t => t.Name === thargoidLevel) as OverwatchThargoidLevel);
    }
    else {
      this.thargoidLevelsSelected = [...data.Levels];
    }
    this.hideUnpopulated = (await this.appService.getSetting("SystemListHideUnpopulated")) == "1";
    this.hideCompleted = (await this.appService.getSetting("SystemListHideCompleted")) == "1";
    const optionalColumnsSetting = await this.appService.getSetting("SystemListOptionalColumns");
    if (optionalColumnsSetting) {
      this.optionalColumns = optionalColumnsSetting.split(",");
    }
    const systemListFeatures = await this.appService.getSetting("SystemListFeatures");
    if (systemListFeatures) {
      this.featuresSelected = systemListFeatures.split(",");
    }
    this.changeDetectorRef.markForCheck();
  }

  public async toggleFeature(feature: string): Promise<void> {
    if (this.featuresSelected.includes(feature) && this.featuresSelected.length === 1) {
      for (const f of this.features) {
        if (!this.featuresSelected.includes(f.key)) {
          this.featuresSelected.push(f.key);
        }
      }
    }
    else {
      if (this.featuresSelected.length) {
        this.featuresSelected.splice(0, this.featuresSelected.length);
      }
      this.featuresSelected.push(feature);
    }
    this.settingChanged();
  }

  public async toggleThargoidLevel(thargoidLevel: OverwatchThargoidLevel): Promise<void> {
    if (this.thargoidLevelsSelected.length == this.thargoidLevels.length) {
      this.thargoidLevelsSelected.splice(0, this.thargoidLevelsSelected.length);
      this.thargoidLevelsSelected.push(thargoidLevel);
    }
    else {
      const index = this.thargoidLevelsSelected.findIndex(t => t == thargoidLevel)
      if (index !== -1) {
        this.thargoidLevelsSelected.splice(index, 1);
        if (this.thargoidLevelsSelected.length === 0) {
          for (const level of this.thargoidLevels) {
            this.thargoidLevelsSelected.push(level);
          }
        }
      }
      else {
        this.thargoidLevelsSelected.push(thargoidLevel);
      }
    }
    this.settingChanged();
  }

  public async toggleThargoidTitan(titan: OverwatchMaelstrom): Promise<void> {
    if (this.maelstromsSelected.length === this.maelstroms.length) {
      this.maelstromsSelected.splice(0, this.maelstromsSelected.length);
      this.maelstromsSelected.push(titan);
    }
    else {
      const index = this.maelstromsSelected.findIndex(m => m == titan)
      if (index !== -1) {
        this.maelstromsSelected.splice(index, 1);
        if (this.maelstromsSelected.length === 0) {
          for (const maelstrom of this.maelstroms) {
            this.maelstromsSelected.push(maelstrom);
          }
        }
      }
      else {
        this.maelstromsSelected.push(titan);
      }
    }
    this.settingChanged();
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
      await this.appService.deleteSetting("SystemListOptionalColumns");
    }
    if (this.featuresSelected.length === this.features.length) {
      await this.appService.deleteSetting("SystemListFeatures");
    }
    else {
      await this.appService.saveSetting("SystemListFeatures", this.featuresSelected.join(","));
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
    this.maelstromsSelected = [...this.maelstroms];
    this.thargoidLevelsSelected = [...this.thargoidLevels];
    this.featuresSelected = this.features.map(f => f.key);
    if (this.systemList) {
      this.systemList.updateDataSource();
    }
    await this.appService.deleteSetting("Maelstroms");
    await this.appService.deleteSetting("ThargoidLevels");
    await this.appService.deleteSetting("SystemListHideUnpopulated");
    await this.appService.deleteSetting("SystemListHideCompleted");
    await this.appService.deleteSetting("SystemListFeatures");
  }
}

interface OverwatchOverviewV2 {
  PreviousCycle: OverwatchOverviewV2Cycle;
  PreviousCycleChanges: OverwatchOverviewV2CycleChange;
  CurrentCycle: OverwatchOverviewV2Cycle;
  NextCycleChanges: OverwatchOverviewV2CycleChange;
  NextCyclePrediction: OverwatchOverviewV2Cycle;
  Maelstroms: OverwatchMaelstrom[];
  Levels: OverwatchThargoidLevel[];
  Systems: OverwatchStarSystemFull[];
  NextTick: string;
  Status: OverviewDataStatus;
}

enum OverviewDataStatus {
  Default,
  TickInProgress,
  UpdatePending,
}

export interface OverwatchThargoidCycle {
  Cycle: string;
  Start: string;
  StartDate: string;
  End: string;
  EndDate: string;
  IsCurrent: boolean;
}