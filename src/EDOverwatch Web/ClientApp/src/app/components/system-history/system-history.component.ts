import { AfterViewInit, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { faMagnifyingGlassChart } from '@fortawesome/free-solid-svg-icons';
import { OverwatchStarSystemFullDetail, OverwatchStarSystemThargoidLevelHistory } from '../system/system.component';

@Component({
  selector: 'app-system-history',
  templateUrl: './system-history.component.html',
  styleUrls: ['./system-history.component.css']
})
export class SystemHistoryComponent implements OnChanges, AfterViewInit {
  public readonly faMagnifyingGlassChart = faMagnifyingGlassChart;
  @ViewChild(MatSort) sort!: MatSort;
  @Input() starSystem!: OverwatchStarSystemFullDetail;
  public history: MatTableDataSource<OverwatchStarSystemThargoidLevelHistory> = new MatTableDataSource<OverwatchStarSystemThargoidLevelHistory>();
  public historyDisplayedColumns = ['ThargoidLevel', 'StateStart', 'StateEnds', 'StateIngameTimerExpires', 'ProgressPercentage'];


  public ngOnChanges(): void {
    this.history = new MatTableDataSource<OverwatchStarSystemThargoidLevelHistory>(this.starSystem.StateHistory);
    this.history.sortingDataAccessor = (item: OverwatchStarSystemThargoidLevelHistory, columnName: string): string | number => {
      switch (columnName) {
        case "ThargoidLevel": {
          return item.ThargoidLevel.Name;
        }
      }
      return item[columnName as keyof OverwatchStarSystemThargoidLevelHistory] as string | number;
    }
    this.history.sort = this.sort;
  }

  public ngAfterViewInit(): void {
    this.history.sort = this.sort;
  }
}
