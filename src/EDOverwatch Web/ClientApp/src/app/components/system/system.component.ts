import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Component } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { OverwatchStarSystem } from '../system-list/system-list.component';
import { OverwatchStation } from '../station-name/station-name.component';
import { Chart, ChartConfiguration, ChartDataset, ChartEvent, ChartType, ChartTypeRegistry } from 'chart.js';

import { AnnotationOptions, default as Annotation } from 'chartjs-plugin-annotation';

@UntilDestroy()
@Component({
  selector: 'app-system',
  templateUrl: './system.component.html',
  styleUrls: ['./system.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemComponent implements AfterViewInit {
  public readonly faClipboard = faClipboard;
  public starSystem: OverwatchStarSystemDetail | null = null;
  public lineChartData: ChartConfiguration['data'] = {
    datasets: [],
    labels: [],
  };

  public lineChartOptions: ChartConfiguration['options'] = {
    elements: {
      line: {
        tension: 0.5
      }
    },
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      // We use this empty structure as a placeholder for dynamic theming.
      y: {
        position: 'left',
      }
      /*
      y1: {
        position: 'right',
        grid: {
          color: 'rgba(255,0,0,0.3)',
        },
        ticks: {
          color: 'red'
        }
      }
      */
    },
    plugins: {
      legend: { display: true },
      annotation: {
        annotations: [],
      }
    }
  };

  public lineChartType: ChartType = 'line';

  public constructor(
    private readonly route: ActivatedRoute,
    private readonly websocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar
  ) {
    Chart.register(Annotation)
  }

  public ngAfterViewInit(): void {
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
      let labels: string[] = [];
      for (const warEffort of this.starSystem.WarEfforts) {
        if (!labels.includes(warEffort.Date)) {
          labels.push(warEffort.Date);
        }
      }
      labels = labels.sort();
      const totalAmounts: {
        [key: string]: Int32Array
      } = {};
      for (const warEffort of this.starSystem.WarEfforts) {
        if (!totalAmounts[warEffort.TypeGroup]) {
          totalAmounts[warEffort.TypeGroup] = new Int32Array(labels.length);
        }
        const index = labels.findIndex(l => l === warEffort.Date);
        totalAmounts[warEffort.TypeGroup][index] += warEffort.Amount;
      }

      const datasets: ChartDataset[] = [];
      for (const label in totalAmounts) {
        datasets.push({
          label: label,
          data: Array.from(totalAmounts[label]),
        });
      }

      const annotations: AnnotationOptions[] = [
        {
          type: 'line',
          scaleID: 'x',
          value: this.starSystem.LastTickDate,
          borderColor: 'orange',
          borderWidth: 2,
          label: {
            display: true,
            position: 'center',
            color: 'orange',
            content: 'Weekly tick'
          }
        },
      ];

      this.lineChartData.labels = labels;
      this.lineChartData.datasets = datasets;
      this.lineChartOptions!.plugins!.annotation!.annotations = annotations;
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

  public chartClicked({ event, active }: { event?: ChartEvent, active?: {}[] }): void {
    console.log(event, active);
  }

  public chartHovered({ event, active }: { event?: ChartEvent, active?: {}[] }): void {
    console.log(event, active);
  }
}

export interface OverwatchStarSystemDetail extends OverwatchStarSystem {
  Population: number;
  WarEfforts: OverwatchStarSystemWarEffort[];
  FactionOperationDetails: FactionOperation[];
  Stations: OverwatchStation[];
  LastTickTime: string;
  LastTickDate: string;
}

export interface OverwatchStarSystemWarEffort {
  Date: string;
  Type: string;
  TypeId: number,
  TypeGroup: string;
  Source: string;
  SourceId: number;
  Amount: number;
}

export interface FactionOperation {
  Faction: string;
  Type: string;
  Started: string;
  SystemName: string;
  SystemAddress: number;
}
