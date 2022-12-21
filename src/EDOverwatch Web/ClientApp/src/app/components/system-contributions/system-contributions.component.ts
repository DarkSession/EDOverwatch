import { AfterViewInit, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { OverwatchStarSystemDetail, OverwatchStarSystemWarEffort } from '../system/system.component';

@Component({
  selector: 'app-system-contributions',
  templateUrl: './system-contributions.component.html',
  styleUrls: ['./system-contributions.component.css']
})
export class SystemContributionsComponent implements OnChanges, AfterViewInit {
  public warEfforts: MatTableDataSource<OverwatchStarSystemWarEffort> = new MatTableDataSource<OverwatchStarSystemWarEffort>();
  public readonly warEffortsDisplayedColumns = ['Date', 'Source', 'Type', 'Amount'];
  @ViewChild(MatSort) sort!: MatSort;

  @Input() starSystem!: OverwatchStarSystemDetail;

  public ngOnChanges(): void {
    this.warEfforts = new MatTableDataSource<OverwatchStarSystemWarEffort>(this.starSystem.WarEfforts);
    this.warEfforts.sort = this.sort;
    this.warEfforts.sortingDataAccessor = (warEffort: OverwatchStarSystemWarEffort, columnName: string): string => {
      return warEffort[columnName as keyof OverwatchStarSystemWarEffort] as string;
    }
  }

  public ngAfterViewInit(): void {
    this.warEfforts.sort = this.sort;
  }
}
