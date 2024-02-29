import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, OnInit, ViewChild } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort, Sort, SortDirection } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { faClipboard, faHexagonExclamation } from '@fortawesome/pro-light-svg-icons';
import { faCircleCheck } from '@fortawesome/free-solid-svg-icons';
import { AppService } from 'src/app/services/app.service';
import { OverwatchMaelstrom, OverwatchMaelstromProgress } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';
import { mkConfig, generateCsv, download } from "export-to-csv";
import { faFileCsv, faCircleQuestion, faTruck, faKitMedical } from '@fortawesome/pro-duotone-svg-icons';
import { faCrosshairs, faHandshake } from '@fortawesome/pro-light-svg-icons';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';

@UntilDestroy()
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
  public readonly faKitMedical = faKitMedical;
  public readonly faTruck = faTruck;
  public readonly faHandshake = faHandshake;
  public readonly faHexagonExclamation = faHexagonExclamation;
  private readonly baseColumns = ['Name', 'ThargoidLevel', 'Population', 'Starports', 'Progress', 'Features', 'FactionOperations', 'StateExpiration', 'Maelstrom'];
  public displayedColumns: string[] = [];
  @ViewChild(MatSort, { static: true }) sort!: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator | null = null;
  @Input() systems: OverwatchStarSystemFull[] = [];
  @Input() maelstromsSelected: OverwatchMaelstrom[] | null = null;
  @Input() thargoidLevelsSelected: OverwatchThargoidLevel[] | null = null;
  @Input() featuresSelected: string[] | null = null;
  @Input() systemNameFilter: string | null = null;
  @Input() maxHeight: number | null = null;
  @Input() hideUnpopulated: boolean = false;
  @Input() hideCompleted: boolean = false;
  @Input() optionalColumns: string[] = [];
  public pageSize: number = 50;
  public dataSource: MatTableDataSource<OverwatchStarSystemFull> = new MatTableDataSource<OverwatchStarSystemFull>();
  public sortColumn: string = "Progress";
  public sortDirection: SortDirection = "desc";
  public filterApplied = false;
  public editSaving = false;

  public constructor(
    private readonly appService: AppService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar,
    private readonly websocketService: WebsocketService
  ) {
    this.initDataSource([]);
  }

  public ngOnInit(): void {
    this.appService.onEditPermissionsChanged
      .pipe(untilDestroyed(this))
      .subscribe(() => {
        this.ngOnChanges();
      });
    this.updateSettings();
  }

  public sortData(sort: Sort): void {
    this.appService.updateTableSort("SystemList", sort.active, sort.direction);
  }

  private async updateSettings(): Promise<void> {
    const sort = await this.appService.getTableSort("SystemList", this.sortColumn, this.sortDirection);
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

  public ngOnChanges(): void {
    this.updateDataSource();
    this.displayedColumns = [...this.baseColumns, ...this.optionalColumns];
    if (this.appService.editPermissions) {
      this.displayedColumns.push("AdminOptionSystemInCounterstrike");
    }
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
      (d.Progress !== 100 || !this.hideCompleted) &&
      this.isFeatureFilterSelected(d));
    this.filterApplied = data.length < this.systems.length;
    if (this.sort?.active) {
      data = this.dataSource.sortData(data, this.sort);
    }
    for (const entry of data) {
      entry.editCounterstrike = entry.Features?.includes("Counterstrike") ?? false;
    }
    this.initDataSource(data);
    this.changeDetectorRef.detectChanges();
  }

  private isFeatureFilterSelected(system: OverwatchStarSystemFull): boolean {
    if (this.featuresSelected === null) {
      return true;
    }
    else if (system.Features.length === 0 && this.featuresSelected.includes("None")) {
      return true;
    }
    for (const featureSelected of this.featuresSelected) {
      if (system.Features.includes(featureSelected)) {
        return true;
      }
    }
    return false;
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
        case "Progress": {
          return system.StateProgress.ProgressPercent ?? 0;
        }
        case "ProgressReportedCompletion": {
          return (system.StateProgress.IsCompleted) ? system.StateProgress.ProgressCompletionReported : Number.MIN_SAFE_INTEGER;
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
        ProgressCompletionReported: system.StateProgress.IsCompleted ? system.StateProgress.ProgressCompletionReported : null,
        ProgressPercent: system.StateProgress.ProgressPercent,
        ProgressUncapped: system.StateProgress.ProgressUncapped,
        ProgressLastChange: system.StateProgress.ProgressLastChange,
        NextSystemState: system.StateProgress.NextSystemState?.Name ?? "",
        NextSystemStateChanges: system.StateProgress.SystemStateChanges ?? "",
        Focus: system.EffortFocus,
        FactionOperations: system.FactionOperations,
        SpecialFactionOperations: system.SpecialFactionOperations.map(s => s.Tag).join(","),
        StationsUnderRepair: system.StationsUnderRepair,
        StationsDamaged: system.StationsDamaged,
        StationsUnderAttack: system.StationsUnderAttack,
        ThargoidSpireSiteInSystem: system.ThargoidSpireSiteInSystem,
        ThargoidSpireSiteBody: system.ThargoidSpireSiteBody ?? "",
        Features: system.Features?.join(","),
      });
    }

    const csvConfig = mkConfig({
      fieldSeparator: ',',
      quoteStrings: true,
      decimalSeparator: '.',
      showTitle: false,
      filename: "Overwatch System List Export",
      useTextFile: false,
      useBom: true,
      useKeysAsHeaders: true,
    });

    const csv = generateCsv(csvConfig)(data);
    download(csvConfig)(csv);
  }

  public async saveSystem(system: OverwatchStarSystem): Promise<void> {
    if (!this.appService.editPermissions || this.editSaving) {
      return;
    }
    this.editSaving = true;
    this.changeDetectorRef.markForCheck();
    await this.websocketService.sendMessageAndWaitForResponse("AdminSystemUpdate", {
      SystemAddress: system.SystemAddress,
      IsCounterstrikeSystem: !!system.editCounterstrike,
    });
    this.editSaving = false;
    this.changeDetectorRef.markForCheck();
  }
}

export interface OverwatchStarSystem {
  PopulationOriginal: number;
  SystemAddress: number;
  Name: string;
  Coordinates: OverwatchStarSystemCoordinates;
  Maelstrom: OverwatchMaelstromProgress;
  ThargoidLevel: OverwatchThargoidLevel;
  Progress: number | null;
  ProgressPercent: number | null;
  StateExpiration: OverwatchStarSystemStateExpires | null;
  StateProgress: StateProgress;
  Population: number;
  DistanceToMaelstrom: number;
  ThargoidSpireSiteInSystem: boolean;
  ThargoidSpireSiteBody: string | null;
  editCounterstrike?: boolean;
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
  ProgressUncapped: number;
  IsCompleted: boolean;
  NextSystemState: OverwatchThargoidLevel | null;
  SystemStateChanges: string;
  ProgressLastChange: string;
  ProgressLastChecked: string;
  ProgressCompletionReported: string;
}