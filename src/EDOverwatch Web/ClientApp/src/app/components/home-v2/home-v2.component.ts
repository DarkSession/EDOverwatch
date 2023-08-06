import { AfterContentInit, AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchOverviewV2Cycle } from '../home-v2-cycle/home-v2-cycle.component';
import { OverwatchOverviewV2CycleChange } from '../home-v2-cycle-changes/home-v2-cycle-changes.component';
import { faArrowUpRightFromSquare, faCircleXmark, faFilter } from '@fortawesome/free-solid-svg-icons';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';
import { OverwatchStarSystem, SystemListComponent } from '../system-list/system-list.component';
import { AppService } from 'src/app/services/app.service';

@UntilDestroy()
@Component({
  selector: 'app-home-v2',
  templateUrl: './home-v2.component.html',
  styleUrls: ['./home-v2.component.scss']
})
export class HomeV2Component implements OnInit, AfterViewInit {
  public readonly faArrowUpRightFromSquare = faArrowUpRightFromSquare;
  public readonly faFilter = faFilter;
  public readonly faCircleXmark = faCircleXmark;
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
  public dataRaw: OverwatchStarSystem[] = [];
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
        this.changeDetectorRef.detectChanges();
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

interface OverwatchOverviewV2 {
  PreviousCycle: OverwatchOverviewV2Cycle;
  PreviousCycleChanges: OverwatchOverviewV2CycleChange;
  CurrentCycle: OverwatchOverviewV2Cycle;
  NextCycleChanges: OverwatchOverviewV2CycleChange;
  NextCyclePrediction: OverwatchOverviewV2Cycle;
  Maelstroms: OverwatchMaelstrom[];
  Levels: OverwatchThargoidLevel[];
  Systems: OverwatchStarSystem[];
  NextTick: string;
}