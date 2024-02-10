import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchStarSystem } from '../system-list/system-list.component';
import { MatTableDataSource } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { faClipboard } from '@fortawesome/pro-light-svg-icons';
import { MatSort } from '@angular/material/sort';
import { download, generateCsv, mkConfig } from 'export-to-csv';
import { faFileCsv } from '@fortawesome/pro-duotone-svg-icons';

@UntilDestroy()
@Component({
  selector: 'app-spire-sites',
  templateUrl: './spire-sites.component.html',
  styleUrl: './spire-sites.component.css'
})
export class SpireSitesComponent implements OnInit {
  public systemsDataSource: MatTableDataSource<OverwatchStarSystem> = new MatTableDataSource<OverwatchStarSystem>();
  @ViewChild(MatSort) sort!: MatSort;
  public readonly systemDataSourceColumns = ['System', 'ThargoidLevel', 'SiteBody', 'Titan', 'Distance'];
  public readonly faClipboard = faClipboard;
  public readonly faFileCsv = faFileCsv;

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly webSocketService: WebsocketService,
    private readonly matSnackBar: MatSnackBar
  ) {
  }

  public ngOnInit(): void {
    this.webSocketService
      .on<OverwatchSpireSites>("OverwatchSpireSites")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.update(message.Data);
      });
    this.webSocketService
      .onReady
      .pipe(untilDestroyed(this))
      .subscribe((isReconnect: boolean) => {
        if (isReconnect) {
          this.webSocketService.sendMessage("OverwatchSpireSites", {});
        }
      });
    this.webSocketService.sendMessage("OverwatchSpireSites", {});
  }

  private update(data: OverwatchSpireSites): void {
    this.systemsDataSource = new MatTableDataSource<OverwatchStarSystem>(data.Systems);
    this.systemsDataSource.sortingDataAccessor = (d: OverwatchStarSystem, columnName: string): string | number => {
      switch (columnName) {
        case "System": {
          return d.Name;
        }
        case "SiteBody": {
          return d.ThargoidSpireSiteBody ?? "";
        }
        case "Titan": {
          return d.Maelstrom.Name;
        }
        case "ThargoidLevel": {
          return d.ThargoidLevel.Name;
        }
        case "Distance": {
          return d.DistanceToMaelstrom;
        }
      }
      return d[columnName as keyof OverwatchStarSystem] as string | number;
    }
    this.systemsDataSource.sort = this.sort;
    this.changeDetectorRef.detectChanges();
  }

  public copySystemName(data: OverwatchStarSystem): void {
    navigator.clipboard.writeText(data.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }

  public exportToCsv(): void {
    const data = [];
    for (const system of this.systemsDataSource.data) {
      data.push({
        Name: system.Name,
        SystemAddress: system.SystemAddress,
        X: system.Coordinates.X,
        Y: system.Coordinates.Y,
        Z: system.Coordinates.Z,
        Maelstrom: system.Maelstrom.Name,
        DistanceToMaelstrom: system.DistanceToMaelstrom,
        State: system.ThargoidLevel.Name,
        StateExpires: system.StateExpiration?.StateExpires ?? "",
        Progress: system.Progress ?? 0,
        ProgressIsCompleted: system.StateProgress.IsCompleted,
        ProgressPercent: system.StateProgress.ProgressPercent,
        ProgressUncapped: system.StateProgress.ProgressUncapped,
        ThargoidSpireSiteBody: system.ThargoidSpireSiteBody ?? "",
      });
    }

    const csvConfig = mkConfig({
      fieldSeparator: ',',
      quoteStrings: true,
      decimalSeparator: '.',
      showTitle: false,
      filename: "Overwatch Spire Sites Export",
      useTextFile: false,
      useBom: true,
      useKeysAsHeaders: true,
    });

    const csv = generateCsv(csvConfig)(data);
    download(csvConfig)(csv);
  }

}

interface OverwatchSpireSites {
  Systems: OverwatchStarSystem[];
}