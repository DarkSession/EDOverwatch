import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, OnInit, ViewChild } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort, Sort, SortDirection } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { faCircleQuestion, faClipboard } from '@fortawesome/free-regular-svg-icons';
import { faCircle, faCircleCheck, faCrosshairs, faFileCsv, faPlus, faTruck } from '@fortawesome/free-solid-svg-icons';
import { AppService } from 'src/app/services/app.service';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';
import { ExportToCsv, Options } from 'export-to-csv';

@Component({
  selector: 'app-system-list',
  templateUrl: './system-list.component.html',
  styleUrls: ['./system-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemListComponent implements OnInit, OnChanges {
  public readonly faClipboard = faClipboard;
  public readonly faCircleCheck = faCircleCheck;
  public readonly faFileCsv = faFileCsv;
  public readonly faCircleQuestion = faCircleQuestion;
  public readonly faCrosshairs = faCrosshairs;
  public readonly faPlus = faPlus;
  public readonly faTruck = faTruck;
  public readonly faCircle = faCircle;
  private readonly baseColumns = ['Name', 'ThargoidLevel', 'Population', 'Starports', 'Progress', 'Features', 'FactionOperations', 'StateExpiration', 'Maelstrom'];
  public displayedColumns: string[] = [];
  @ViewChild(MatSort, { static: true }) sort!: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator | null = null;
  @Input() systems: OverwatchStarSystemFull[] = [];
  @Input() maelstromsSelected: OverwatchMaelstrom[] | null = null;
  @Input() thargoidLevelsSelected: OverwatchThargoidLevel[] | null = null;
  @Input() systemNameFilter: string | null = null;
  @Input() maxHeight: number | null = null;
  @Input() hideUnpopulated: boolean = false;
  @Input() hideCompleted: boolean = false;
  @Input() optionalColumns: string[] = [];
  public pageSize: number = 50;
  public dataSource: MatTableDataSource<OverwatchStarSystemFull> = new MatTableDataSource<OverwatchStarSystemFull>();
  public sortColumn: string = "Progress";
  public sortDirection: SortDirection = "desc";
  public progressShowPercentage = true;
  public filterApplied = false;

  public constructor(
    private readonly appService: AppService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar
  ) {
    this.initDataSource([]);
  }

  public ngOnInit(): void {
    this.updateSettings();
  }

  public sortData(sort: Sort): void {
    this.appService.updateTableSort("SystemList", sort.active, sort.direction);
  }

  private async updateSettings(): Promise<void> {
    const sort = await this.appService.getTableSort("SystemList", this.sortColumn, this.sortDirection);
    const pageSizeSetting = await this.appService.getSetting("SystemListPageSize");
    this.progressShowPercentage = !(await this.appService.getSetting("SystemListProgressShowPercentage") === "0");
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

  public ngOnChanges(): void {
    this.updateDataSource();
    this.displayedColumns = [...this.baseColumns, ...this.optionalColumns];
  }

  public async handlePageEvent(e: PageEvent): Promise<void> {
    if (e.pageSize) {
      this.pageSize = e.pageSize;
      this.appService.saveSetting("SystemListPageSize", e.pageSize.toString());
    }
  }

  public updateDataSource(): void {
    const systemNameFilter = (this.systemNameFilter ?? "").trim().toUpperCase();
    let data = this.systems.filter(d =>
      (this.maelstromsSelected === null || typeof this.maelstromsSelected.find(m => m.Name === d.Maelstrom.Name) !== 'undefined') &&
      (this.thargoidLevelsSelected === null || typeof this.thargoidLevelsSelected.find(t => t.Level === d.ThargoidLevel.Level) !== 'undefined') &&
      (systemNameFilter === "" || d.Name.toUpperCase().includes(systemNameFilter)) &&
      (d.PopulationOriginal > 0 || !this.hideUnpopulated) &&
      (d.Progress !== 100 || !this.hideCompleted));
    this.filterApplied = data.length < this.systems.length;
    if (this.sort?.active) {
      data = this.dataSource.sortData(data, this.sort);
    }
    this.initDataSource(data);
    this.changeDetectorRef.detectChanges();
  }

  private initDataSource(data: OverwatchStarSystemFull[]) {
    this.dataSource = new MatTableDataSource<OverwatchStarSystemFull>(data);
    this.dataSource.paginator = this.paginator;
    this.dataSource.sortingDataAccessor = (system: OverwatchStarSystemFull, columnName: string): string | number => {
      switch (columnName) {
        case "ThargoidLevel": {
          return system.ThargoidLevel.Level;
        }
        case "Maelstrom": {
          return system.Maelstrom.Name;
        }
        case "Starports": {
          return (system.StationsUnderAttack + system.StationsDamaged + system.StationsUnderRepair);
        }
        /*
        case "FactionOperations": {
          return (system.FactionOperations + system.SpecialFactionOperations.length * 9);
        }
        */
        case "ProgressReportedCompletion": {
          return (system.StateProgress.IsCompleted) ? system.StateProgress.ProgressLastChange : Number.MIN_SAFE_INTEGER;
        }
        case "StateExpiration": {
          return (system.StateExpiration?.StateExpires ?? "");
        }
        case "Features": {
          return system.Features?.length ?? 0;
        }
      }
      return system[columnName as keyof OverwatchStarSystem] as string | number;
    }
    this.dataSource.sort = this.sort;
  }

  public copySystemName(starSystem: OverwatchStarSystem): void {
    navigator.clipboard.writeText(starSystem.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }

  public toggleProgressShowPercentage(): void {
    this.progressShowPercentage = !this.progressShowPercentage;
    this.appService.saveSetting("SystemListProgressShowPercentage", this.progressShowPercentage ? "1" : "0");
  }

  public exportToCsv(all: boolean): void {
    const data = [];
    const systems = all ? this.systems : this.dataSource.data;
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
        StateExpires: system.StateExpiration?.StateExpires ?? "",
        Progress: system.Progress ?? 0,
        ProgressIsCompleted: system.StateProgress.IsCompleted,
        ProgressLastChange: system.StateProgress.ProgressLastChange,
        NextSystemState: system.StateProgress.NextSystemState?.Name ?? "",
        NextSystemStateChanges: system.StateProgress.SystemStateChanges ?? "",
        Focus: system.EffortFocus,
        FactionOperations: system.FactionOperations,
        SpecialFactionOperations: system.SpecialFactionOperations.map(s => s.Tag).join(","),
        StationsUnderRepair: system.StationsUnderRepair,
        StationsDamaged: system.StationsDamaged,
        StationsUnderAttack: system.StationsUnderAttack,
        BarnacleMatrixInSystem: system.BarnacleMatrixInSystem,
      });
    }

    const options: Options = {
      fieldSeparator: ',',
      quoteStrings: '"',
      decimalSeparator: '.',
      showLabels: true,
      showTitle: false,
      filename: "Overwatch System List Export",
      useTextFile: false,
      useBom: true,
      useKeysAsHeaders: true,
    };

    const csvExporter = new ExportToCsv(options);
    csvExporter.generateCsv(data);
  }
}

export interface OverwatchStarSystem {
  PopulationOriginal: number;
  SystemAddress: number;
  Name: string;
  Coordinates: OverwatchStarSystemCoordinates;
  Maelstrom: OverwatchMaelstrom;
  ThargoidLevel: OverwatchThargoidLevel;
  Progress: number | null;
  ProgressPercent: number | null;
  StateExpiration: OverwatchStarSystemStateExpires | null;
  StateProgress: StateProgress;
  Population: number;
  DistanceToMaelstrom: number;
  BarnacleMatrixInSystem: boolean;
}

export interface OverwatchStarSystemFull extends OverwatchStarSystem {
  EffortFocus: number;
  FactionOperations: number;
  FactionAxOperations: number;
  FactionGeneralOperations: number;
  FactionRescueOperations: number;
  FactionLogisticsOperations: number;
  SpecialFactionOperations: OverwatchStarSystemSpecialFactionOperation[];
  StationsUnderRepair: number;
  StationsDamaged: number;
  StationsUnderAttack: number;
  Features: string[];
}

export interface OverwatchStarSystemCoordinates {
  X: number;
  Y: number;
  Z: number;
}

interface OverwatchStarSystemSpecialFactionOperation {
  Tag: string;
  Name: string;
}

interface OverwatchStarSystemStateExpires {
  StateExpires: string;
  CurrentCycleEnds: string;
  RemainingCycles: number;
}

interface StateProgress {
  ProgressPercent: number;
  IsCompleted: boolean;
  NextSystemState: OverwatchThargoidLevel | null;
  SystemStateChanges: string;
  ProgressLastChange: string;
  ProgressLastChecked: string;
}