import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import * as dayjs from 'dayjs';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';
import { ChartConfiguration, ChartDataset, Color } from 'chart.js';
import { Context } from 'chartjs-plugin-datalabels';

@UntilDestroy()
@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['home.component.scss'],
})
export class HomeComponent implements OnInit, OnDestroy {
  public start: dayjs.Dayjs = dayjs("2022-11-29T18:30:00.000+00:00");
  public timeSince: string = "";
  private updateInterval: any = null;
  public overview: OverwatchOverview | null = null;
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
          display: true,
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

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly webSocketService: WebsocketService
  ) {
  }

  public ngOnInit(): void {
    this.updateInterval = setInterval(() => {
      this.updateTimeSince();
    }, 60000);
    this.webSocketService
      .on<OverwatchOverview>("OverwatchHome")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.overview = message.Data;
        this.processChartData();
        this.changeDetectorRef.detectChanges();
      });
    this.webSocketService
      .onReady
      .pipe(untilDestroyed(this))
      .subscribe((isReconnect: boolean) => {
        if (isReconnect) {
          this.webSocketService.sendMessage("OverwatchHome", {});
        }
      });
    this.updateTimeSince();
    this.webSocketService.sendMessage("OverwatchHome", {});
  }

  private processChartData(): void {
    if (this.overview && this.overview.ThargoidCycles && this.overview.MaelstromHistory) {
      const labels = this.overview.ThargoidCycles.map(t => t.Cycle);
      const datasets: ChartDataset<'line', number[]>[] = [];

      for (const maelstromHistory of this.overview.MaelstromHistory) {
        if (maelstromHistory.State.Name === "Controlled") {
          let dataset = datasets.find(d => d.label === maelstromHistory.State.Name && d.type === 'line');
          if (!dataset) {
            dataset = {
              label: maelstromHistory.State.Name,
              data: this.overview.ThargoidCycles.map(t => 0),
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
              data: this.overview.ThargoidCycles.map(t => 0),
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
              color: 'white',
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

  public ngOnDestroy(): void {
    if (this.updateInterval) {
      clearInterval(this.updateInterval);
      this.updateInterval = null;
    }
  }

  private updateTimeSince(): void {
    const results: string[] = [];
    const now = dayjs.utc();
    const duration = dayjs.duration(now.diff(this.start));

    if (duration.years() === 1) {
      results.push("1 year");
    }
    else if (duration.years() > 1) {
      results.push(`${duration.years()} years`)
    }

    if (duration.months() === 1) {
      results.push("1 month");
    }
    else if (duration.months() > 1) {
      results.push(`${duration.months()} months`)
    }

    if (duration.days() === 1) {
      results.push("1 day");
    }
    else if (duration.days() > 1) {
      results.push(`${duration.days()} days`)
    }

    if (duration.asMonths() < 1) {
      if (duration.hours() === 1) {
        results.push("1 hour");
      }
      else if (duration.hours() > 1) {
        results.push(`${duration.hours()} hours`)
      }
    }
    this.timeSince = results.join(", ");
    this.changeDetectorRef.detectChanges();
  }
}

interface OverwatchOverview {
  Humans: OverwatchOverviewHuman;
  Thargoids: OverwatchOverviewThargoids;
  Contested: OverwatchOverviewContested;
  MaelstromHistory: OverwatchOverviewMaelstromHistoricalSummary[];
  ThargoidCycles: OverwatchThargoidCycle[];
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

export interface OverwatchThargoidCycle {
  Cycle: string;
  Start: string;
  StartDate: string;
  End: string;
  EndDate: string;
  IsCurrent: boolean;
}

export interface OverwatchOverviewMaelstromHistoricalSummary {
  Cycle: OverwatchThargoidCycle;
  Maelstrom: OverwatchMaelstrom;
  State: OverwatchThargoidLevel;
  Amount: number;
}
