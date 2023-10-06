import { ChangeDetectionStrategy, ChangeDetectorRef, OnInit } from '@angular/core';
import { Component } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';
import { faClipboard, faCrosshairsSimple, faHexagonExclamation } from '@fortawesome/pro-light-svg-icons';
import { OverwatchStarSystem, OverwatchStarSystemFull } from '../system-list/system-list.component';
import { OverwatchStarSystemMin, OverwatchStation } from '../station-name/station-name.component';
import { ChartConfiguration, ChartDataset, ChartType } from 'chart.js';
import { AnnotationOptions } from 'chartjs-plugin-annotation';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';
import { Context } from 'chartjs-plugin-datalabels';
import { faBolt } from '@fortawesome/free-solid-svg-icons';
import { faCrosshairs } from '@fortawesome/pro-light-svg-icons';

@UntilDestroy()
@Component({
  selector: 'app-system',
  templateUrl: './system.component.html',
  styleUrls: ['./system.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemComponent implements OnInit {
  public readonly faClipboard = faClipboard;
  public readonly faBolt = faBolt;
  public readonly faHexagonExclamation = faHexagonExclamation;
  public readonly faCrosshairs = faCrosshairs;
  public readonly faCrosshairsSimple = faCrosshairsSimple;
  public starSystem: OverwatchStarSystemFullDetail | null = null;
  public lineChartData: ChartConfiguration['data'] = {
    datasets: [],
    labels: [],
  };
  public barnacleMatrix = false;
  public odysseySettlement = false;
  public federation = false;
  public empire = false;
  public thargoidControlledReactivationMissions = false;
  public aXConflictZones = false;
  public groundPortAXCZ = false;

  public lineChartOptions: ChartConfiguration['options'] = {
    elements: {
      line: {
        tension: 0.1,
      },
    },
    // responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: {
        position: 'left',
      },
      y1: {
        min: 0,
        max: 100,
        position: 'right',
        display: false,
        ticks: {
          color: '#e95e03'
        }
      }
    },
    interaction: {
      mode: 'index',
      intersect: false,
    },
    plugins: {
      legend: { display: true },
      annotation: {
        annotations: [],
      },
      datalabels: {
        align: 'center',
        anchor: 'center',
        color: 'black',
        backgroundColor: (context: Context) => {
          return '#e95e03';
        },
        display: (context) => {
          return context.dataset.label === "Progress";
        },
        borderRadius: 4,
      }
    }
  };
  public lineChartType: ChartType = 'line';
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
        const systemId = parseInt(p.get("id") ?? "0");
        if (systemId && this.starSystem?.SystemAddress != systemId) {
          this.websocketService.sendMessage("OverwatchSystem", {
            SystemAddress: systemId,
          });
        }
      });
    this.websocketService.on<OverwatchStarSystemFullDetail>("OverwatchSystem")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message && message.Data) {
          this.starSystem = message.Data;
          let labels: string[] = message.Data.DaysSincePreviousTick ?? [];
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
            const lastDate = this.starSystem.ProgressDetails.filter(p => p.Date === label).sort((a, b) => (a.DateTime < b.DateTime) ? 1 : -1);
            if (lastDate.length > 0) {
              const endOfDayProgress = lastDate[0].Progress ?? 0;
              progress.push(endOfDayProgress);
              previousProgress = endOfDayProgress;
            }
            else {
              progress.push(previousProgress);
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
                content: 'Weekly tick',
              }
            });
          }

          this.lineChartData = {
            datasets: datasets,
            labels: labels,
          };
          this.lineChartOptions!.plugins!.annotation!.annotations = annotations;
          if (this.chartLoaded) {
            this.lineChartOptions!.animation = false;
          }

          this.barnacleMatrix = this.starSystem?.Features?.includes("BarnacleMatrix") ?? false;
          this.odysseySettlement = this.starSystem?.Features?.includes("OdysseySettlements") ?? false;
          this.federation = this.starSystem?.Features?.includes("FederalFaction") ?? false;
          this.empire = this.starSystem?.Features?.includes("ImperialFaction") ?? false;
          this.thargoidControlledReactivationMissions = this.starSystem?.Features?.includes("ThargoidControlledReactivationMissions") ?? false;
          this.aXConflictZones = this.starSystem?.Features?.includes("AXConflictZones") ?? false;
          this.groundPortAXCZ = this.starSystem?.Features?.includes("GroundPortAXCZ") ?? false;
          this.changeDetectorRef.markForCheck();
          this.chartLoaded = true;
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

  public encodeUrlPart(part: string): string {
    return encodeURIComponent(part);
  }
}

export interface OverwatchStarSystemFullDetail extends OverwatchStarSystemFull {
  WarEfforts: OverwatchStarSystemWarEffort[];
  ProgressDetails: OverwatchStarSystemDetailProgress[];
  FactionOperationDetails: FactionOperation[];
  Stations: OverwatchStation[];
  LastTickTime: string;
  LastTickDate: string;
  WarEffortSources: OverwatchStarSystemWarEffortType[];
  StateHistory: OverwatchStarSystemThargoidLevelHistory[];
  WarEffortSummaries: OverwatchStarSystemWarEffortCycle[];
  NearbySystems: OverwatchStarSystemNearbySystem[];
  DaysSincePreviousTick: string[];
  AttackDefense: OverwatchStarSystemAttackDefense;
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
  MeetingPoint: string | null;
}

export interface OverwatchStarSystemNearbySystem {
  StarSystem: OverwatchStarSystem;
  Distance: number,
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

export interface OverwatchStarSystemWarEffortCycle {
  CycleStart: string;
  CycleEnd: string;
  EffortTotals: OverwatchStarSystemWarEffortCycleEntry[];
}

interface OverwatchStarSystemWarEffortCycleEntry {
  Type: string;
  TypeId: number;
  Source: string;
  SourceId: number;
  TypeGroup: string;
  Amount: number;
}

export interface OverwatchStarSystemAttackDefense {
  RecentAttacker: OverwatchStarSystemMin | null;
  PredictedAttacker: OverwatchStarSystemMin | null;
  RecentlyAttacked: OverwatchStarSystemMin | null;
  PredictedAttack: OverwatchStarSystemMin | null;
  RequirementsTissueSampleTotal: number | null;
  RequirementsTissueSampleRemaining: number | null;
}