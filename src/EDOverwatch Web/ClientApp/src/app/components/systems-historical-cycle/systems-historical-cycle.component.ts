import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchStarSystemCoordinates, OverwatchStarSystemFull } from '../system-list/system-list.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';
import { WebsocketService } from 'src/app/services/websocket.service';
import { AppService } from 'src/app/services/app.service';
import { MatSort, Sort, SortDirection } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { faClipboard } from '@fortawesome/pro-light-svg-icons';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { faCircleCheck } from '@fortawesome/free-solid-svg-icons';
import { mkConfig, generateCsv, download } from "export-to-csv";
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { faFilters, faFileCsv, faCircleXmark } from '@fortawesome/pro-duotone-svg-icons';
import { OverwatchThargoidCycle } from '../home-v2/home-v2.component';

@UntilDestroy()
@Component({
  selector: 'app-systems-historical-cycle',
  templateUrl: './systems-historical-cycle.component.html',
  styleUrls: ['./systems-historical-cycle.component.scss']
})
export class SystemsHistoricalCycleComponent implements OnInit {
  public readonly faClipboard = faClipboard;
  public readonly faCircleCheck = faCircleCheck;
  public readonly faFileCsv = faFileCsv;
  public readonly faFilters = faFilters;
  public readonly faCircleXmark = faCircleXmark;
  @ViewChild(MatSort, { static: true }) sort!: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator | null = null;
  public dateLoaded: string | null = "01-01-2000";
  public date: string | null = "";
  public dateSelectionDisabled = false;
  public dataSource: MatTableDataSource<OverwatchStarSystemsHistoricalSystem> = new MatTableDataSource<OverwatchStarSystemsHistoricalSystem>();
  public sortColumn: string = "Name";
  public sortDirection: SortDirection = "asc";
  public readonly displayedColumns = ['Name', 'ThargoidLevel', 'PopulationOriginal', 'Progress', 'StateExpires', 'Maelstrom'];
  public pageSize: number = 50;
  public activeCycle: OverwatchThargoidCycle | null = null;
  public thargoidCycles: OverwatchThargoidCycle[] = [];
  public systemNameFilter: string = "";
  public maelstroms: OverwatchMaelstrom[] = [];
  public maelstromsSelected: OverwatchMaelstrom[] = [];
  public thargoidLevels: OverwatchThargoidLevel[] = [];
  public thargoidLevelsSelected: OverwatchThargoidLevel[] = [];
  public systems: OverwatchStarSystemsHistoricalSystem[] = [];
  public filterApplied = false;
  public loading = false;

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly webSocketService: WebsocketService,
    private readonly appService: AppService,
    private readonly matSnackBar: MatSnackBar,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
  }

  public ngOnInit(): void {
    this.route.paramMap
      .pipe(untilDestroyed(this))
      .subscribe((p: ParamMap) => {
        let date = p.get("date");
        if (!date) {
          date = null;
        }
        this.requestSystemData(date);
      });
    this.updateSettings();
  }

  public dateChanged(): void {
    if (this.date) {
      this.router.navigate(['/', 'systems-cycle', this.date]);
    }
  }

  public async handlePageEvent(e: PageEvent): Promise<void> {
    if (e.pageSize) {
      this.pageSize = e.pageSize;
      this.appService.saveSetting("SystemListPageSize", e.pageSize.toString());
    }
  }

  private async updateSettings(): Promise<void> {
    const sort = await this.appService.getTableSort("SystemListHistorical", this.sortColumn, this.sortDirection);
    const pageSizeSetting = await this.appService.getSetting("SystemListPageSize");
    this.sortColumn = sort.Column;
    this.sortDirection = sort.Direction;
    if (pageSizeSetting) {
      const pageSize = parseInt(pageSizeSetting);
      if (!isNaN(pageSize) && pageSize >= 10) {
        this.pageSize = pageSize;
      }
    }
    if (this.sort?.active) {
      this.sort.active = sort.Column;
      this.sort.direction = sort.Direction;
      this.dataSource.data = this.dataSource.sortData(this.dataSource.data, this.sort);
    }
    this.changeDetectorRef.markForCheck();
  }

  public sortData(sort: Sort): void {
    this.appService.updateTableSort("SystemListHistorical", sort.active, sort.direction);
  }

  public copySystemName(starSystem: OverwatchStarSystemsHistoricalSystem): void {
    navigator.clipboard.writeText(starSystem.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }

  private async requestSystemData(cycle: string | null): Promise<void> {
    if (this.dateLoaded == cycle) {
      return;
    }
    this.dateSelectionDisabled = true;
    this.loading = true;
    this.changeDetectorRef.detectChanges();
    const response = await this.webSocketService.sendMessageAndWaitForResponse<OverwatchStarSystemsHistorical>("OverwatchSystemsHistoricalCycle", {
      Cycle: cycle,
      DefaultWeek: -1,
      IgnoreClear: true,
    });
    if (response && response.Data) {
      const data = response.Data as OverwatchStarSystemsHistorical;
      this.thargoidCycles = response.Data.ThargoidCycles;
      this.date = response.Data.Cycle.Cycle;
      this.dateLoaded = response.Data.Cycle.Cycle;
      this.systems = response.Data.Systems;
      this.activeCycle = response.Data.Cycle;
      this.maelstroms = data.Maelstroms.sort((a, b) => (a.Name > b.Name) ? 1 : -1);
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
      this.updateDataSource();
    }
    this.loading = true;
    this.changeDetectorRef.detectChanges();
  }

  public exportToCsv(): void {
    const data = [];
    const systems = this.dataSource.data;
    for (const system of systems) {
      data.push({
        Name: system.Name,
        SystemAddress: system.SystemAddress,
        X: system.Coordinates.X,
        Y: system.Coordinates.Y,
        Z: system.Coordinates.Z,
        Maelstrom: system.Maelstrom.Name,
        DistanceToMaelstrom: system.DistanceToMaelstrom,
        Population: system.Population,
        PopulationOriginal: system.PopulationOriginal,
        State: system.ThargoidLevel.Name,
        StateExpires: system.StateExpires ?? "",
        Progress: system.Progress ?? 0,
        ProgressPercent: system.ProgressPercent ?? 0,
        ProgressIsCompleted: system.ProgressIsCompleted,
        ThargoidSpireSiteInSystem: system.ThargoidSpireSiteInSystem,
      });
    }

    const csvConfig = mkConfig({
      fieldSeparator: ',',
      quoteStrings: true,
      decimalSeparator: '.',
      showTitle: false,
      filename: "Overwatch Historical System List Export",
      useTextFile: false,
      useBom: true,
      useKeysAsHeaders: true,
    });

    const csv = generateCsv(csvConfig)(data);
    download(csvConfig)(csv);
  }

  public updateDataSource(): void {
    const systemNameFilter = (this.systemNameFilter ?? "").trim().toUpperCase();
    let data = this.systems.filter(d =>
      (this.maelstromsSelected === null || typeof this.maelstromsSelected.find(m => m.Name === d.Maelstrom.Name) !== 'undefined') &&
      (this.thargoidLevelsSelected === null || typeof this.thargoidLevelsSelected.find(t => t.Level === d.ThargoidLevel.Level) !== 'undefined') &&
      (systemNameFilter === "" || d.Name.toUpperCase().includes(systemNameFilter)));
    this.filterApplied = data.length < this.systems.length;
    if (this.sort?.active) {
      data = this.dataSource.sortData(data, this.sort);
    }
    this.dataSource = new MatTableDataSource<OverwatchStarSystemsHistoricalSystem>(data);
    this.dataSource.paginator = this.paginator;
    this.dataSource.sortingDataAccessor = (system: OverwatchStarSystemsHistoricalSystem, columnName: string): string | number => {
      switch (columnName) {
        case "ThargoidLevel": {
          return system.ThargoidLevel.Level;
        }
        case "Maelstrom": {
          return system.Maelstrom.Name;
        }
        case "StateExpires": {
          return (system.StateExpires ?? "");
        }
      }
      return system[columnName as keyof OverwatchStarSystemsHistoricalSystem] as string | number;
    }
    this.dataSource.sort = this.sort;
    this.changeDetectorRef.detectChanges();
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
    this.updateDataSource();
  }

  public async resetFilter(): Promise<void> {
    this.maelstromsSelected = this.maelstroms;
    this.thargoidLevelsSelected = this.thargoidLevels;
    this.updateDataSource();
    await this.appService.deleteSetting("Maelstroms");
    await this.appService.deleteSetting("ThargoidLevels");
  }
}

export interface OverwatchStarSystemsHistorical {
  Maelstroms: OverwatchMaelstrom[];
  Levels: OverwatchThargoidLevel[];
  ThargoidCycles: OverwatchThargoidCycle[];
  Systems: OverwatchStarSystemsHistoricalSystem[];
  Cycle: OverwatchThargoidCycle;
}

interface OverwatchStarSystemsHistoricalSystem {
  SystemAddress: number;
  Name: string;
  Coordinates: OverwatchStarSystemCoordinates;
  Maelstrom: OverwatchMaelstrom;
  Population: number;
  ThargoidLevel: OverwatchThargoidLevel;
  PreviousThargoidLevel: OverwatchThargoidLevel;
  State: string;
  PopulationOriginal: number;
  DistanceToMaelstrom: number;
  ThargoidSpireSiteInSystem: boolean;
  Progress: number | null;
  ProgressPercent: number | null;
  ProgressIsCompleted: boolean;
  StateExpires: string | null;
}

export interface OverwatchSystems {
  Maelstroms: OverwatchMaelstrom[];
  Levels: OverwatchThargoidLevel[];
  Systems: OverwatchStarSystemFull[];
  NextTick: string;
}