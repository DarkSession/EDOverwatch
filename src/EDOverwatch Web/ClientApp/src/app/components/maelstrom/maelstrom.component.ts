import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { ChartConfiguration, ChartDataset, Color } from 'chart.js';
import { Context } from 'chartjs-plugin-datalabels';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchMaelstromProgress } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchStarSystemFull } from '../system-list/system-list.component';
import { OverwatchAlertPredictionMaelstrom } from '../alert-prediction/alert-prediction.component';
import { OverwatchOverviewMaelstromHistoricalSummary } from '../stats/stats.component';
import { OverwatchThargoidCycle } from '../home-v2/home-v2.component';

@UntilDestroy()
@Component({
  selector: 'app-maelstrom',
  templateUrl: './maelstrom.component.html',
  styleUrls: ['./maelstrom.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MaelstromComponent implements OnInit {
  public maelstrom: OverwatchMaelstromDetail | null = null;
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
  public canvasWidth = 800;

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
          this.processChartData();
          this.changeDetectorRef.markForCheck();
        }
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
      this.canvasWidth = 100 + this.maelstrom.ThargoidCycles.length * 30;
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

interface OverwatchMaelstromDetail extends OverwatchMaelstromProgress {
  Systems: OverwatchStarSystemFull[];
  MaelstromHistory: OverwatchOverviewMaelstromHistoricalSummary[];
  ThargoidCycles: OverwatchThargoidCycle[];
  AlertPrediction: OverwatchAlertPredictionMaelstrom;
}

