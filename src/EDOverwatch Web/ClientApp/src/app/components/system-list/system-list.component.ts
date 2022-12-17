import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchThargoidLevel } from '../systems/systems.component';

@Component({
  selector: 'app-system-list',
  templateUrl: './system-list.component.html',
  styleUrls: ['./system-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemListComponent implements OnChanges {
  public readonly faClipboard = faClipboard;
  public readonly displayedColumns = ['Name', 'ThargoidLevel', 'Maelstrom', 'Progress', 'EffortFocus', 'FactionOperations'];
  @ViewChild(MatSort, { static: true }) sort!: MatSort;
  @Input() systems: OverwatchStarSystem[] = [];
  @Input() maelstromsSelected: OverwatchMaelstrom[] | null = null;
  @Input() thargoidLevelsSelected: OverwatchThargoidLevel[] | null = null;
  @Input() maxHeight: number | null = null;
  public dataSource: MatTableDataSource<OverwatchStarSystem> = new MatTableDataSource<OverwatchStarSystem>();

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar) {
  }

  public ngOnChanges(): void {
    this.updateDataSource();
  }

  public updateDataSource(): void {
    let data = this.systems.filter(d =>
      (this.maelstromsSelected === null || typeof this.maelstromsSelected.find(m => m.Name === d.Maelstrom.Name) !== 'undefined') &&
      (this.thargoidLevelsSelected === null || typeof this.thargoidLevelsSelected.find(t => t.Level === d.ThargoidLevel.Level) !== 'undefined'));
    if (!this.sort?.active) {
      this.dataSource.data.sort((system1: OverwatchStarSystem, system2: OverwatchStarSystem) => {
        if (system1.ThargoidLevel.Level > system2.ThargoidLevel.Level) {
          return -1;
        }
        else if (system1.ThargoidLevel.Level < system2.ThargoidLevel.Level) {
          return 1;
        }
        return system1.Name.localeCompare(system2.Name);
      });
    }
    else {
      data = this.dataSource.sortData(data, this.sort);
    }
    this.dataSource = new MatTableDataSource<OverwatchStarSystem>(data);
    this.dataSource.sort = this.sort;
    this.dataSource.sortingDataAccessor = (system: OverwatchStarSystem, columnName: string): string => {
      switch (columnName) {
        case "ThargoidLevel": {
          return system.ThargoidLevel.Name;
        }
        case "Maelstrom": {
          return system.Maelstrom.Name;
        }
      }
      return system[columnName as keyof OverwatchStarSystem] as string;
    }

    this.changeDetectorRef.detectChanges();
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
  EffortFocus: number;
  FactionOperations: number;
}
