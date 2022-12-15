import { ChangeDetectionStrategy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { Component, OnInit } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchStarSystem } from '../systems/systems.component';

@UntilDestroy()
@Component({
  selector: 'app-system',
  templateUrl: './system.component.html',
  styleUrls: ['./system.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemComponent implements OnInit {
  public starSystem: OverwatchStarSystemDetail | null = null;
  public warEfforts: MatTableDataSource<OverwatchStarSystemWarEffort> = new MatTableDataSource<OverwatchStarSystemWarEffort>();
  public readonly displayedColumns = ['Date', 'Source', 'Type', 'Amount'];
  @ViewChild(MatSort) sort!: MatSort;

  public constructor(
    private readonly route: ActivatedRoute,
    private readonly websocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.route.paramMap
      .pipe(untilDestroyed(this))
      .subscribe((p: ParamMap) => {
        const systemId = parseInt(p.get("id") ?? "0");
        if (systemId && this.starSystem?.SystemAddress != systemId) {
          this.requestSystem(systemId);
        }
      });
  }

  private async requestSystem(systemAddress: number): Promise<void> {
    const response = await this.websocketService.sendMessageAndWaitForResponse<OverwatchStarSystemDetail>("OverwatchSystem", {
      SystemAddress: systemAddress,
    });
    if (response && response.Data) {
      this.starSystem = response.Data;
      this.warEfforts = new MatTableDataSource<OverwatchStarSystemWarEffort>(this.starSystem.WarEfforts);
      this.warEfforts.sort = this.sort;
      this.warEfforts.sortingDataAccessor = (warEffort: OverwatchStarSystemWarEffort, columnName: string): string => {
        return warEffort[columnName as keyof OverwatchStarSystemWarEffort] as string;
      }
      this.changeDetectorRef.markForCheck();
    }
  }
}

interface OverwatchStarSystemDetail extends OverwatchStarSystem {
  Population: number;
  WarEfforts: OverwatchStarSystemWarEffort[];
}

interface OverwatchStarSystemWarEffort {
  Date: string;
  Type: string;
  Source: string;
  Amount: number;
}