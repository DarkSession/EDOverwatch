import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { ChartConfiguration, ChartDataset, Color } from 'chart.js';
import { Context } from 'chartjs-plugin-datalabels';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchOverviewMaelstromHistoricalSummary, OverwatchThargoidCycle } from '../home/home.component';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchStarSystem } from '../system-list/system-list.component';
import { OverwatchStarSystemMin } from '../station-name/station-name.component';

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
  public alertPredictions: MatTableDataSource<OverwatchMaelstromDetailAlertPrediction> = new MatTableDataSource<OverwatchMaelstromDetailAlertPrediction>();
  public readonly alertPredictionColumns = ['Name', 'Population', 'Distance', 'Attackers'];
  @ViewChild(MatSort, { static: false }) sort!: MatSort;
  public chartConfig: ChartConfiguration = {
    type: 'bar',
    data: {
      datasets: [],
      labels: [],
    },
    options: {
      responsive: true,
      plugins: {
        legend: {
          position: 'bottom',
        }
      },
      scales: {
        y1: {
          type: 'linear',
          display: false,
          position: 'right',
          // grid line settings
          grid: {
            drawOnChartArea: true, // only want the grid lines for one axis to show up
          },
        },
        y: {
          position: 'left',
        },
      }
    },
  };
  private chartLoaded: boolean = false;

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
          this.websocketService.sendMessage("OverwatchMaelstrom", {
            Name: name,
          });
        }
      });
    this.websocketService.on<OverwatchMaelstromDetail>("OverwatchMaelstrom")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message && message.Data) {
          const data = message.Data;
          this.maelstrom = data;
          const sortedAlertPredictions = (this.sort) ? this.alertPredictions.sortData(data.AlertPredictions, this.sort) : data.AlertPredictions;
          this.alertPredictions = new MatTableDataSource<OverwatchMaelstromDetailAlertPrediction>(sortedAlertPredictions);
          this.alertPredictions.sortingDataAccessor = (system: OverwatchMaelstromDetailAlertPrediction, columnName: string): string => {
            return system[columnName as keyof OverwatchMaelstromDetailAlertPrediction] as unknown as string;
          }
          this.alertPredictions.sort = this.sort;
          this.processChartData();
          this.changeDetectorRef.markForCheck();
        }
      });
  }

  public copySystemName(starSystem: OverwatchMaelstromDetailAlertPrediction): void {
    navigator.clipboard.writeText(starSystem.StarSystem.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }

  private processChartData(): void {
    if (this.maelstrom && this.maelstrom.ThargoidCycles && this.maelstrom.MaelstromHistory) {
      const labels = this.maelstrom.ThargoidCycles.map(t => t.Cycle);
      const datasets: ChartDataset<'line', number[]>[] = [];

      for (const maelstromHistory of this.maelstrom.MaelstromHistory) {
        if (maelstromHistory.State.Name === "Controlled") {
          let dataset = datasets.find(d => d.label === maelstromHistory.State.Name && d.type === 'line');
          if (!dataset) {
            dataset = {
              label: maelstromHistory.State.Name,
              data: this.maelstrom.ThargoidCycles.map(t => 0),
              type: 'line',
              yAxisID: 'y1',
              order: 0,
              // borderWidth: 8,
              segment: {
                borderWidth: 8,
              }
            };
            this.updateDatasetColor(dataset);
            datasets.push(dataset);
          }
          const index = labels.findIndex(c => c === maelstromHistory.Cycle.Cycle);
          if (index !== -1) {
            dataset.data[index] += maelstromHistory.Amount;
          }
        }
        else {
          let dataset = datasets.find(d => d.label === maelstromHistory.State.Name);
          if (!dataset) {
            dataset = {
              label: maelstromHistory.State.Name,
              data: this.maelstrom.ThargoidCycles.map(t => 0),
              yAxisID: 'y',
              order: 1,
              stack: 'stack',
            };
            this.updateDatasetColor(dataset);
            datasets.push(dataset);
          }
          const index = labels.findIndex(c => c === maelstromHistory.Cycle.Cycle);
          if (index !== -1) {
            dataset.data[index] += maelstromHistory.Amount;
          }
        }
      }

      this.chartConfig = {
        type: 'bar',
        data: {
          datasets: [...datasets],
          labels: labels,
        },
        options: {
          responsive: true,
          plugins: {
            legend: {
              position: 'bottom',
            },
            datalabels: {
              align: 'center',
              anchor: 'center',
              color: (context) => {
                return context.dataset.label === "Controlled" ? "white" : "black";
              },
              backgroundColor: (context: Context) => {
                return context.dataset.backgroundColor as Color;
              },
              display: (context) => {
                return !!context.dataset.data[context.dataIndex];
              },
              borderRadius: 4,
            }
          },
          interaction: {
            mode: 'index',
            intersect: false,
          },
          scales: {
            y1: {
              type: 'linear',
              position: 'right',
              display: false,
            },
            y: {
              position: 'left',
              stacked: true,
            },
          },
        },
      };
      if (this.chartLoaded) {
        this.chartConfig.options!.animation = false;
      }
      this.chartLoaded = true;
    }
  }

  private updateDatasetColor(dataset: ChartDataset<'line', number[]>) {
    switch (dataset.label) {
      case "Alert": {
        dataset.backgroundColor = "#f1c232";
        break;
      }
      case "Invasion": {
        dataset.backgroundColor = "#ff5200";
        break;
      }
      case "Controlled": {
        dataset.backgroundColor = "#38761d";
        break;
      }
      case "Recovery": {
        dataset.backgroundColor = "#9f1bff";
        break;
      }
    }
    dataset.borderColor = dataset.backgroundColor;
    dataset.pointBackgroundColor = dataset.backgroundColor;
    dataset.pointBorderColor = dataset.backgroundColor;
  }
}

interface OverwatchMaelstromDetail extends OverwatchMaelstrom {
  Systems: OverwatchStarSystem[];
  AlertPredictions: OverwatchMaelstromDetailAlertPrediction[];
  MaelstromHistory: OverwatchOverviewMaelstromHistoricalSummary[];
  ThargoidCycles: OverwatchThargoidCycle[];
}

interface OverwatchMaelstromDetailAlertPrediction {
  StarSystem: OverwatchStarSystemMin;
  Distance: number;
  Attackers: OverwatchMaelstromDetailAlertPredictionAttacker[];
}

interface OverwatchMaelstromDetailAlertPredictionAttacker {
  StarSystem: OverwatchStarSystem;
  Distance: number;
}