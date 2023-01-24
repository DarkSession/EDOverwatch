import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, OnInit, ViewChild } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort, Sort, SortDirection } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { faCircleCheck } from '@fortawesome/free-solid-svg-icons';
import { AppService } from 'src/app/services/app.service';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';

@Component({
  selector: 'app-system-list',
  templateUrl: './system-list.component.html',
  styleUrls: ['./system-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemListComponent implements OnInit, OnChanges {
  public readonly faClipboard = faClipboard;
  public readonly faCircleCheck = faCircleCheck;
  private readonly baseColumns = ['Name', 'ThargoidLevel', 'Population', 'Starports', 'Maelstrom', 'Progress', 'FactionOperations', 'StateExpiration'];
  public displayedColumns = ['Name', 'ThargoidLevel', 'Population', 'Starports', 'Maelstrom', 'Progress', 'EffortFocus', 'FactionOperations', 'StateExpiration'];
  @ViewChild(MatSort, { static: true }) sort!: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator | null = null;
  @Input() systems: OverwatchStarSystem[] = [];
  @Input() maelstromsSelected: OverwatchMaelstrom[] | null = null;
  @Input() thargoidLevelsSelected: OverwatchThargoidLevel[] | null = null;
  @Input() systemNameFilter: string | null = null;
  @Input() maxHeight: number | null = null;
  @Input() hideUnpopulated: boolean = false;
  @Input() hideCompleted: boolean = false;
  @Input() optionalColumns: string[] = [];
  public pageSize: number = 50;
  public dataSource: MatTableDataSource<OverwatchStarSystem> = new MatTableDataSource<OverwatchStarSystem>();
  public sortColumn: string = "FactionOperations";
  public sortDirection: SortDirection = "desc";
  public progressShowPercentage = false;

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
    this.sortColumn = sort.Column;
    this.sortDirection = sort.Direction;
    const pageSizeSetting = await this.appService.getSetting("SystemListPageSize");
    if (pageSizeSetting) {
      const pageSize = parseInt(pageSizeSetting);
      if (!isNaN(pageSize) && pageSize >= 10) {
        this.pageSize = pageSize;
      }
    }
    this.progressShowPercentage = (await this.appService.getSetting("SystemListProgressShowPercentage") === "1");
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
    if (this.sort?.active) {
      data = this.dataSource.sortData(data, this.sort);
    }
    this.initDataSource(data);
    this.changeDetectorRef.detectChanges();
  }

  private initDataSource(data: OverwatchStarSystem[]) {
    this.dataSource = new MatTableDataSource<OverwatchStarSystem>(data);
    this.dataSource.paginator = this.paginator;
    this.dataSource.sortingDataAccessor = (system: OverwatchStarSystem, columnName: string): string | number => {
      switch (columnName) {
        case "ThargoidLevel": {
          return system.ThargoidLevel.Name;
        }
        case "Maelstrom": {
          return system.Maelstrom.Name;
        }
        case "Starports": {
          return (system.StationsUnderAttack + system.StationsDamaged + system.StationsUnderRepair);
        }
        case "FactionOperations": {
          return (system.FactionOperations + system.SpecialFactionOperations.length * 10);
        }
        case "StateExpiration": {
          return (system.StateExpiration?.StateExpires ?? "");
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
  EffortFocus: number;
  FactionOperations: number;
  SpecialFactionOperations: OverwatchStarSystemSpecialFactionOperation[];
  StationsUnderRepair: number;
  StationsDamaged: number;
  StationsUnderAttack: number;
  StateExpiration: OverwatchStarSystemStateExpires | null;
  StateProgress: StateProgress;
  Population: number;
  DistanceToMaelstrom: number;
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
}