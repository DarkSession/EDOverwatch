import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, OnInit, ViewChild } from '@angular/core';
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
  public readonly displayedColumns = ['Name', 'ThargoidLevel', 'Starports', 'Maelstrom', 'Progress', 'EffortFocus', 'FactionOperations', 'StateExpiration'];
  @ViewChild(MatSort, { static: true }) sort!: MatSort;
  @Input() systems: OverwatchStarSystem[] = [];
  @Input() maelstromsSelected: OverwatchMaelstrom[] | null = null;
  @Input() thargoidLevelsSelected: OverwatchThargoidLevel[] | null = null;
  @Input() systemNameFilter: string | null = null;
  @Input() maxHeight: number | null = null;
  public dataSource: MatTableDataSource<OverwatchStarSystem> = new MatTableDataSource<OverwatchStarSystem>();
  public sortColumn: string = "FactionOperations";
  public sortDirection: SortDirection = "desc";

  public constructor(
    private readonly appService: AppService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar) {
    this.initDataSource([]);
  }

  public ngOnInit(): void {
    this.updateSort();
  }

  public sortData(sort: Sort): void {
    this.appService.updateTableSort("SystemList", sort.active, sort.direction);
  }

  private async updateSort(): Promise<void> {
    const sort = await this.appService.getTableSort("SystemList", this.sortColumn, this.sortDirection);
    this.sortColumn = sort.Column;
    this.sortDirection = sort.Direction;
    this.changeDetectorRef.markForCheck();
  }

  public ngOnChanges(): void {
    this.updateDataSource();
  }

  public updateDataSource(): void {
    const systemNameFilter = (this.systemNameFilter ?? "").trim().toUpperCase();
    let data = this.systems.filter(d =>
      (this.maelstromsSelected === null || typeof this.maelstromsSelected.find(m => m.Name === d.Maelstrom.Name) !== 'undefined') &&
      (this.thargoidLevelsSelected === null || typeof this.thargoidLevelsSelected.find(t => t.Level === d.ThargoidLevel.Level) !== 'undefined') &&
      (systemNameFilter === "" || d.Name.toUpperCase().includes(systemNameFilter)));
    if (this.sort?.active) {
      data = this.dataSource.sortData(data, this.sort);
    }
    this.initDataSource(data);
    this.changeDetectorRef.detectChanges();
  }

  private initDataSource(data: OverwatchStarSystem[]) {
    this.dataSource = new MatTableDataSource<OverwatchStarSystem>(data);
    this.dataSource.sortingDataAccessor = (system: OverwatchStarSystem, columnName: string): string | number => {
      switch (columnName) {
        case "ThargoidLevel": {
          return system.ThargoidLevel.Name;
        }
        case "Maelstrom": {
          return system.Maelstrom.Name;
        }
        case "Starports": {
          return (system.StationsUnderAttack + system.StationsUnderRepair);
        }
        case "FactionOperations": {
          return (system.FactionOperations + system.SpecialFactionOperations.length * 100);
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
}

export interface OverwatchStarSystem {
  SystemAddress: number;
  Name: string;
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