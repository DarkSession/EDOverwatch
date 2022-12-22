import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';

@UntilDestroy()
@Component({
  selector: 'app-my-efforts',
  templateUrl: './my-efforts.component.html',
  styleUrls: ['./my-efforts.component.css']
})
export class MyEffortsComponent implements OnInit {
  public readonly displayedColumns = ['Date', 'SystemName', 'Type', 'Amount'];
  public dataSource: MatTableDataSource<CommanderWarEffort> = new MatTableDataSource<CommanderWarEffort>();
  @ViewChild(MatSort) sort!: MatSort;

  public constructor(
    private readonly webSocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.webSocketService
      .on<CommanderWarEffort[]>("CommanderWarEfforts")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.initEfforts(message.Data);
      });
    this.webSocketService.sendMessage("CommanderWarEfforts", {});
  }

  private initEfforts(data: CommanderWarEffort[]): void {
    this.dataSource = new MatTableDataSource<CommanderWarEffort>(data);
    this.dataSource.sortingDataAccessor = (warEffort: CommanderWarEffort, columnName: string): string => {
      return warEffort[columnName as keyof CommanderWarEffort] as string;
    }
    this.dataSource.sort = this.sort;
    this.changeDetectorRef.detectChanges();
  }
}

interface CommanderWarEffort {
  Date: string;
  Type: string;
  SystemName: string;
  Amount: number;
}