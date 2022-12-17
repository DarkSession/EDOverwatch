import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchStarSystem } from '../system-list/system-list.component';

@UntilDestroy()
@Component({
  selector: 'app-maelstrom',
  templateUrl: './maelstrom.component.html',
  styleUrls: ['./maelstrom.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MaelstromComponent implements OnInit {
  public readonly faClipboard = faClipboard;
  public maelstrom: OverwatchMaelstromDetail | null = null;
  public systemsAtRisk: MatTableDataSource<OverwatchMaelstromDetailSystemAtRisk> = new MatTableDataSource<OverwatchMaelstromDetailSystemAtRisk>();
  public readonly systemsAtRiskColumns = ['Name', 'Population', 'Distance'];
  @ViewChild(MatSort, { static: true }) sort!: MatSort;

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
        const name = p.get("name");
        if (name && this.maelstrom?.Name != name) {
          this.requestMaelstrom(name);
        }
      });
    this.websocketService.on<OverwatchMaelstromDetail>("OverwatchMaelstrom")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message && message.Data) {
          this.updateMaelstrom(message.Data);
        }
      });
  }

  private async requestMaelstrom(name: string): Promise<void> {
    const response = await this.websocketService.sendMessageAndWaitForResponse<OverwatchMaelstromDetail>("OverwatchMaelstrom", {
      Name: name,
    });
    if (response && response.Data) {
      this.updateMaelstrom(response.Data);
    }
  }

  private updateMaelstrom(data: OverwatchMaelstromDetail): void {
    this.maelstrom = data;
    const sortedSystemsAtRisk = (this.sort) ? this.systemsAtRisk.sortData(data.SystemsAtRisk, this.sort) : data.SystemsAtRisk;
    this.systemsAtRisk = new MatTableDataSource<OverwatchMaelstromDetailSystemAtRisk>(sortedSystemsAtRisk);
    this.systemsAtRisk.sort = this.sort;
    this.systemsAtRisk.sortingDataAccessor = (system: OverwatchMaelstromDetailSystemAtRisk, columnName: string): string => {
      return system[columnName as keyof OverwatchMaelstromDetailSystemAtRisk] as string;
    }
    this.changeDetectorRef.markForCheck();
  }

  public copySystemName(starSystem: OverwatchMaelstromDetailSystemAtRisk): void {
    navigator.clipboard.writeText(starSystem.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }
}

interface OverwatchMaelstromDetail extends OverwatchMaelstrom {
  Systems: OverwatchStarSystem[];
  SystemsAtRisk: OverwatchMaelstromDetailSystemAtRisk[];
}

interface OverwatchMaelstromDetailSystemAtRisk {
  Name: string;
  Distance: number;
  Population: number;
}