import { AfterViewInit, Component, Input, OnInit, ViewChild } from '@angular/core';
import { CommanderWarEffortCycleStarSystem, CommanderWarEffortCycleStarSystemWarEffort } from '../my-efforts/my-efforts.component';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';

@Component({
  selector: 'app-my-efforts-cycle-system',
  templateUrl: './my-efforts-cycle-system.component.html',
  styleUrls: ['./my-efforts-cycle-system.component.css']
})
export class MyEffortsCycleSystemComponent implements AfterViewInit {
  public displayedColumns = ["Date", "Type", "Amount"];
  @Input() warEffortCycleStarSystem: CommanderWarEffortCycleStarSystem | null = null;
  @ViewChild(MatSort) sort!: MatSort;
  public dataSource: MatTableDataSource<CommanderWarEffortCycleStarSystemWarEffort> = new MatTableDataSource<CommanderWarEffortCycleStarSystemWarEffort>();

  public ngAfterViewInit(): void {
    if (this.warEffortCycleStarSystem) {
      this.dataSource = new MatTableDataSource<CommanderWarEffortCycleStarSystemWarEffort>(this.warEffortCycleStarSystem.WarEfforts);
      this.dataSource.sortingDataAccessor = (warEffort: CommanderWarEffortCycleStarSystemWarEffort, columnName: string): string => {
        return warEffort[columnName as keyof CommanderWarEffortCycleStarSystemWarEffort] as string;
      }
      this.dataSource.sort = this.sort;
    }
  }
}
