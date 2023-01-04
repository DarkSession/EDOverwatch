import { ChangeDetectionStrategy, ChangeDetectorRef, OnInit } from '@angular/core';
import { Component } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { OverwatchStarSystem } from '../system-list/system-list.component';
import { OverwatchStation } from '../station-name/station-name.component';
import { Chart, ChartConfiguration, ChartDataset, ChartType } from 'chart.js';

import { AnnotationOptions, default as Annotation } from 'chartjs-plugin-annotation';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';

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
      y: {
        position: 'left',
      },
      y1: {
        min: 0,
        max: 100,
        position: 'right',
        grid: {
          color: 'rgba(233, 94, 3, 0.3)',
        },
        ticks: {
          color: '#e95e03'
        }
      }
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
  }

  public ngOnInit(): void {
    Chart.register(Annotation);
    this.route.paramMap
      .pipe(untilDestroyed(this))
      .subscribe((p: ParamMap) => {
        const systemId = parseInt(p.get("id") ?? "0");
        if (systemId && this.starSystem?.SystemAddress != systemId) {
          this.websocketService.sendMessage("OverwatchSystem", {
            SystemAddress: systemId,
          });
        }
      });
    this.websocketService.on<OverwatchStarSystemDetail>("OverwatchSystem")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message && message.Data) {
          this.starSystem = message.Data;
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

          let previousProgress = 0;
          const progress: number[] = [];
          for (const label of labels) {
            const maxNumber = Math.max(...this.starSystem.ProgressDetails.filter(p => p.Date === label).map(p => p.Progress));
            if (isNaN(maxNumber) || maxNumber === Infinity || maxNumber === -Infinity) {
              progress.push(previousProgress);
            }
            else {
              progress.push(maxNumber);
              previousProgress = maxNumber;
            }
          }

          datasets.push({
            label: 'Progress',
            data: progress,
            yAxisID: 'y1',
            backgroundColor: 'rgba(251, 215, 180, 0.2)',
            borderColor: '#f07b05',
            pointBackgroundColor: '#fbd7b4',
            pointBorderColor: '#ffffff',
            pointHoverBackgroundColor: '#ffffff',
            pointHoverBorderColor: 'rgba(233, 94, 3, 0.8)',
            fill: 'origin',
          });

          const annotations: AnnotationOptions[] = [];

          if (labels.includes(this.starSystem.LastTickDate)) {
            annotations.push({
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
            });
          }

          this.lineChartData = {
            datasets: datasets,
            labels: labels,
          };
          this.lineChartOptions!.plugins!.annotation!.annotations = annotations;
          this.changeDetectorRef.markForCheck();
        }
      });
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

export interface OverwatchStarSystemDetail extends OverwatchStarSystem {
  PopulationOriginal: number;
  WarEfforts: OverwatchStarSystemWarEffort[];
  ProgressDetails: OverwatchStarSystemDetailProgress[];
  FactionOperationDetails: FactionOperation[];
  Stations: OverwatchStation[];
  LastTickTime: string;
  LastTickDate: string;
  DistanceToMaelstrom: number;
  WarEffortSources: OverwatchStarSystemWarEffortType[];
  StateHistory: OverwatchStarSystemThargoidLevelHistory[];
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

interface OverwatchStarSystemDetailProgress {
  State: OverwatchThargoidLevel;
  Date: string;
  DateTime: string;
  Progress: number;
  ProgressPercentage: number;
}

interface OverwatchStarSystemWarEffortType {
  TypeId: number;
  Name: string;
}

export interface OverwatchStarSystemThargoidLevelHistory {
  AllowDetailAnalysisDisplay: boolean;
  AnalysisCycle: string;
  ThargoidLevel: OverwatchThargoidLevel;
  StateStart: string;
  StateEnds: string | null;
  StateIngameTimerExpires: string | null;
  Progress: number | null;
  ProgressPercentage: number | null;
}