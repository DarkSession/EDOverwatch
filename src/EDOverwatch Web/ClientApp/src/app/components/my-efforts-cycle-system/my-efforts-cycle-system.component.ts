import { AfterViewInit, Component, Input, ViewChild } from '@angular/core';
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
  public additionalEntries = 0;
  public showAll = false;

  public ngAfterViewInit(): void {
    if (this.warEffortCycleStarSystem) {
      const warEfforts = (this.showAll || this.warEffortCycleStarSystem.WarEfforts.length < 7) ? this.warEffortCycleStarSystem.WarEfforts : [...this.warEffortCycleStarSystem.WarEfforts ].splice(0, 5);
      this.additionalEntries = this.warEffortCycleStarSystem.WarEfforts.length - warEfforts.length;
      this.dataSource = new MatTableDataSource<CommanderWarEffortCycleStarSystemWarEffort>(warEfforts);
      this.dataSource.sortingDataAccessor = (warEffort: CommanderWarEffortCycleStarSystemWarEffort, columnName: string): string => {
        return warEffort[columnName as keyof CommanderWarEffortCycleStarSystemWarEffort] as string;
      }
      this.dataSource.sort = this.sort;
    }
  }

  public toggleShowAll(): void {
    this.showAll = !this.showAll;
    this.ngAfterViewInit();
  }
}
