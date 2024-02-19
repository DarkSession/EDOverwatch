import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { ChartConfiguration, ChartDataset, Color } from 'chart.js';
import { Context } from 'chartjs-plugin-datalabels';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';
import { OverwatchThargoidCycle } from '../home-v2/home-v2.component';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';

@UntilDestroy()
@Component({
  selector: 'app-stats',
  templateUrl: './stats.component.html',
  styleUrls: ['./stats.component.scss']
})
export class StatsComponent implements OnInit {
  public stats: OverwatchWarStats | null = null;
  public contributionSummaryChart: ChartConfiguration = {
    type: 'bar',
    data: {
      datasets: [],
      labels: [],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: {
        mode: 'index',
        intersect: false,
      },
    },
  };
  public maelstromHistoryChart: ChartConfiguration = {
    type: 'bar',
    data: {
      datasets: [],
      labels: [],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
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
        },
        y: {
          position: 'left',
        },
      }
    },
  };
  public clearedSystemsByType: ChartConfiguration = {
    type: 'bar',
    data: {
      datasets: [],
      labels: [],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
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
    private readonly websocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.websocketService
      .on<OverwatchWarStats>("OverwatchWarStats")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message.Data) {
          this.stats = message.Data;
          this.updateCharts();
          this.changeDetectorRef.detectChanges();
        }
      });
    this.websocketService
      .onReady
      .pipe(untilDestroyed(this))
      .subscribe((isReconnect: boolean) => {
        if (isReconnect) {
          this.websocketService.sendMessage("OverwatchWarStats", {});
        }
      });
    this.websocketService.sendMessage("OverwatchWarStats", {});
  }

  private updateCharts(): void {
    if (this.stats) {
      {
        const labels = this.stats.ThargoidCycles.map(t => t.Cycle);
        const datasets: ChartDataset<'bar', number[]>[] = [];
        for (const warEffortSum of this.stats.WarEffortSums) {
          let dataset = datasets.find(d => d.label === warEffortSum.TypeGroup);
          if (!dataset) {
            dataset = {
              label: warEffortSum.TypeGroup,
              data: labels.map(l => 0),
              stack: 'stack',
            };
            datasets.push(dataset);
          }
          const index = labels.findIndex(l => l === warEffortSum.Date);
          if (index !== -1) {
            dataset.data[index] += warEffortSum.Amount;
          }
        }

        this.contributionSummaryChart = {
          type: 'bar',
          data: {
            datasets: datasets,
            labels: labels,
          },
          options: {
            responsive: true,
            interaction: {
              mode: 'index',
              intersect: false,
            },
            plugins: {
              datalabels: {
                display: false,
              }
            }
          },
        };
        if (this.chartLoaded) {
          this.contributionSummaryChart.options!.animation = false;
        }
      }
      {
        const labels = this.stats.ThargoidCycles.map(t => t.Cycle);
        const datasets: ChartDataset<'line', number[]>[] = [];

        {
          const dataset: ChartDataset<'line', number[]> = {
            label: "Cleared",
            data: labels.map(t => 0),
            type: 'line',
            order: 0,
            // borderWidth: 8,
            yAxisID: 'y',
            segment: {
              borderWidth: 8,
            }
          };
          this.updateDatasetColor(dataset);
          datasets.push(dataset);
          for (const completedSystemPerCycle of this.stats.CompletdSystemsPerCycles) {
            const index = labels.findIndex(c => c === completedSystemPerCycle.Cycle);
            if (index !== -1) {
              dataset.data[index] += completedSystemPerCycle.Completed;
            }
          }
        }

        for (const maelstromHistory of this.stats.MaelstromHistory) {
          if (maelstromHistory.State.Name === "Controlled") {
            let dataset = datasets.find(d => d.label === maelstromHistory.State.Name && d.type === 'line');
            if (!dataset) {
              dataset = {
                label: maelstromHistory.State.Name,
                data: labels.map(t => 0),
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
                data: labels.map(t => 0),
                yAxisID: 'y',
                order: 1,
                stack: "stack",
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

        this.maelstromHistoryChart = {
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
                  return context.dataset.label === "Controlled";
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
          this.maelstromHistoryChart.options!.animation = false;
        }
      }
      {

        const labels = this.stats.ThargoidCycles.map(t => t.Cycle);
        const datasets: ChartDataset<'line', number[]>[] = [];

        for (const completedSystemPerCycle of this.stats.CompletdSystemsPerCycles) {
          let dataset = datasets.find(d => d.label === completedSystemPerCycle.State.Name);
          if (!dataset) {
            dataset = {
              label: completedSystemPerCycle.State.Name,
              data: labels.map(t => 0),
              yAxisID: 'y',
              order: 1,
              stack: "stack",
            };
            this.updateDatasetColor(dataset);
            datasets.push(dataset);
          }
          const index = labels.findIndex(c => c === completedSystemPerCycle.Cycle);
          if (index !== -1) {
            dataset.data[index] += completedSystemPerCycle.Completed;
          }
        }

        this.clearedSystemsByType = {
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
          this.clearedSystemsByType.options!.animation = false;
        }
      }
      this.canvasWidth = 100 + this.stats.ThargoidCycles.length * 30;
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
      case "Cleared": {
        dataset.backgroundColor = "#3498db";
        break;
      }
    }
    dataset.borderColor = dataset.backgroundColor;
    dataset.pointBackgroundColor = dataset.backgroundColor;
    dataset.pointBorderColor = dataset.backgroundColor;
  }
}

interface OverwatchWarStats {
  Humans: OverwatchOverviewHuman;
  Thargoids: OverwatchWarStatsThargoids;
  Contested: OverwatchOverviewContested;
  MaelstromHistory: OverwatchOverviewMaelstromHistoricalSummary[];
  WarEffortSums: WarEffortSummary[];
  CompletdSystemsPerCycles: StatsCompletdSystemsPerCycle[];
  ThargoidCycles: OverwatchThargoidCycle[];
}

interface OverwatchWarStatsThargoids extends OverwatchOverviewThargoids {
  SystemsControllingPreviouslyPopulated: number;
}

interface WarEffortSummary {
  CycleNumber: number;
  Date: string;
  TypeId: number;
  Type: string;
  TypeGroup: string;
  Amount: number;
}

interface StatsCompletdSystemsPerCycle {
  Cycle: string;
  Completed: number;
  State: OverwatchThargoidLevel;
}

export interface OverwatchOverviewHuman {
  ControllingPercentage: number;
  SystemsControlling: number;
  SystemsRecaptured: number;
  ThargoidKills: number | null;
  Rescues: number | null;
  RescueSupplies: number | null;
  Missions: number | null;
  ItemsRecovered: number | null;
  SamplesCollected: number | null;
}

export interface OverwatchOverviewThargoids {
  ControllingPercentage: number;
  ActiveMaelstroms: number;
  SystemsControlling: number;
  CommanderKills: number;
  RefugeePopulation: number;
}

export interface OverwatchOverviewContested {
  SystemsInInvasion: number;
  SystemsWithAlerts: number;
  SystemsBeingRecaptured: number;
  SystemsInRecovery: number;
}

export interface OverwatchOverviewMaelstromHistoricalSummary {
  Cycle: OverwatchThargoidCycle;
  Maelstrom: OverwatchMaelstrom;
  State: OverwatchThargoidLevel;
  Amount: number;
}
