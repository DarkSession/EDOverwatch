import { AfterViewInit, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { OverwatchStation } from '../station-name/station-name.component';
import { OverwatchStarSystemFullDetail } from '../system/system.component';

@Component({
  selector: 'app-system-stations',
  templateUrl: './system-stations.component.html',
  styleUrls: ['./system-stations.component.css']
})
export class SystemStationsComponent implements OnChanges, AfterViewInit {
  public stations: MatTableDataSource<OverwatchStation> = new MatTableDataSource<OverwatchStation>();
  public readonly stationsDisplayedColumns = ['Name', 'State', 'RescueShip', 'DistanceFromStarLS'];
  @ViewChild(MatSort) sort!: MatSort;

  @Input() starSystem!: OverwatchStarSystemFullDetail;

  public ngOnChanges(): void {
    this.stations = new MatTableDataSource<OverwatchStation>(this.starSystem.Stations);
    this.stations.sortingDataAccessor = (station: OverwatchStation, columnName: string): string => {
      return station[columnName as keyof OverwatchStation] as string;
    }
    this.stations.sort = this.sort;
  }

  public ngAfterViewInit(): void {
    this.stations.sort = this.sort;
  }
}
