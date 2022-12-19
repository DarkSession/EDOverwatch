import { ChangeDetectionStrategy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { OverwatchStarSystem } from '../system-list/system-list.component';

@UntilDestroy()
@Component({
  selector: 'app-system',
  templateUrl: './system.component.html',
  styleUrls: ['./system.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemComponent implements OnInit {
  public readonly faClipboard = faClipboard;
  public starSystem: OverwatchStarSystemDetail | null = null;

  public warEfforts: MatTableDataSource<OverwatchStarSystemWarEffort> = new MatTableDataSource<OverwatchStarSystemWarEffort>();
  public readonly warEffortsDisplayedColumns = ['Date', 'Source', 'Type', 'Amount'];
  @ViewChild('warEffortsSort') warEffortsSort!: MatSort;

  public factionOperations: MatTableDataSource<FactionOperation> = new MatTableDataSource<FactionOperation>();
  public readonly factionOperationsDisplayedColumns = ['Faction', 'Type', 'Started'];
  @ViewChild('factionOperationsSort') factionOperationsSort!: MatSort;

  public constructor(
    private readonly route: ActivatedRoute,
    private readonly websocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar
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

      this.factionOperations = new MatTableDataSource<FactionOperation>(this.starSystem.FactionOperationDetails);
      this.factionOperations.sort = this.factionOperationsSort;
      this.factionOperations.sortingDataAccessor = (factionOperations: FactionOperation, columnName: string): string => {
        return factionOperations[columnName as keyof FactionOperation] as string;
      }

      this.warEfforts = new MatTableDataSource<OverwatchStarSystemWarEffort>(this.starSystem.WarEfforts);
      this.warEfforts.sort = this.warEffortsSort;
      this.warEfforts.sortingDataAccessor = (warEffort: OverwatchStarSystemWarEffort, columnName: string): string => {
        return warEffort[columnName as keyof OverwatchStarSystemWarEffort] as string;
      }
      this.changeDetectorRef.markForCheck();
    }
  }

  public copySystemName(): void {
    if (!this.starSystem) {
      return;
    }
    navigator.clipboard.writeText(this.starSystem.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }
}

interface OverwatchStarSystemDetail extends OverwatchStarSystem {
  Population: number;
  WarEfforts: OverwatchStarSystemWarEffort[];
  FactionOperationDetails: FactionOperation[];
}

interface OverwatchStarSystemWarEffort {
  Date: string;
  Type: string;
  Source: string;
  Amount: number;
}

interface FactionOperation {
  Faction: string;
  Type: string;
  Started: string;
  SystemName: string;
  SystemAddress: number;
}