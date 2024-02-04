import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { AppService } from 'src/app/services/app.service';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchStarSystem } from '../system-list/system-list.component';
import { MatTableDataSource } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { faClipboard } from '@fortawesome/pro-light-svg-icons';
import { MatSort } from '@angular/material/sort';

@UntilDestroy()
@Component({
  selector: 'app-spire-sites',
  templateUrl: './spire-sites.component.html',
  styleUrl: './spire-sites.component.css'
})
export class SpireSitesComponent implements OnInit {
  public systemsDataSource: MatTableDataSource<OverwatchStarSystem> = new MatTableDataSource<OverwatchStarSystem>();
  @ViewChild(MatSort) sort!: MatSort;
  public readonly systemDataSourceColumns = ['System', 'SiteBody', 'Titan', 'ThargoidLevel'];
  public readonly faClipboard = faClipboard;

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly webSocketService: WebsocketService,
    private readonly appService: AppService,
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
}

interface OverwatchSpireSites {
  Systems: OverwatchStarSystem[];
}