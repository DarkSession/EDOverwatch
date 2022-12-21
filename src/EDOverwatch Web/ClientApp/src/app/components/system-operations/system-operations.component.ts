import { AfterViewInit, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { FactionOperation, OverwatchStarSystemDetail } from '../system/system.component';

@Component({
  selector: 'app-system-operations',
  templateUrl: './system-operations.component.html',
  styleUrls: ['./system-operations.component.css']
})
export class SystemOperationsComponent implements OnChanges, AfterViewInit {
  public factionOperations: MatTableDataSource<FactionOperation> = new MatTableDataSource<FactionOperation>();
  public readonly factionOperationsDisplayedColumns = ['Faction', 'Type', 'Started'];
  @ViewChild(MatSort) sort!: MatSort;

  @Input() starSystem!: OverwatchStarSystemDetail;

  public ngOnChanges(): void {
    this.factionOperations = new MatTableDataSource<FactionOperation>(this.starSystem.FactionOperationDetails);
    this.factionOperations.sort = this.sort;
    this.factionOperations.sortingDataAccessor = (factionOperations: FactionOperation, columnName: string): string => {
      return factionOperations[columnName as keyof FactionOperation] as string;
    }
  }

  public ngAfterViewInit(): void {
    this.factionOperations.sort = this.sort;
  }
}
