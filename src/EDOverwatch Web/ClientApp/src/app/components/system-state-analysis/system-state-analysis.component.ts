import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';

@UntilDestroy()
@Component({
  selector: 'app-system-state-analysis',
  templateUrl: './system-state-analysis.component.html',
  styleUrls: ['./system-state-analysis.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemStateAnalysisComponent implements OnInit {
  public readonly faClipboard = faClipboard;
  public systemCycleAnalysis: OverwatchStarSystemCycleAnalysis | null = null;
  public contributions: MatTableDataSource<Contribution> = new MatTableDataSource<Contribution>();
  public displayedColumns = ['Type'];
  public sources: string[] = [];
  @ViewChild(MatSort) sort!: MatSort;
  public available = true;

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
        const cycle = p.get("cycle");
        if (systemId && cycle && this.systemCycleAnalysis?.SystemAddress !== systemId) {
          this.websocketService.sendMessage("OverwatchSystemAnalysis", {
            SystemAddress: systemId,
            Cycle: cycle,
          });
        }
      });
    this.websocketService.on<OverwatchStarSystemCycleAnalysis>("OverwatchSystemAnalysis")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message && message.Data) {
          this.available = true;
          this.systemCycleAnalysis = message.Data;
          this.sources = [...new Set(this.systemCycleAnalysis.CycleWarEffortsUntilCompleted.map(item => item.Source))];
          this.displayedColumns = ['Type', ...this.sources];
          const contributions: Contribution[] = [];
          for (const warEffort of this.systemCycleAnalysis.CycleWarEffortsUntilCompleted) {
            let contribution = contributions.find(c => c.Type === warEffort.Type);
            if (!contribution) {
              const sourceAmounts = this.sources.map((s) => {
                return { Source: s, Amount: 0 }
              });
              contribution = {
                Type: warEffort.Type,
                SourceAmounts: sourceAmounts,
              };
              contributions.push(contribution);
            }
            const sourceAmount = contribution.SourceAmounts.find(s => s.Source === warEffort.Source);
            if (sourceAmount) {
              sourceAmount.Amount += warEffort.Amount;
            }
          }
          this.contributions = new MatTableDataSource<Contribution>(contributions);
          this.contributions.sort = this.sort;
        }
        else {
          this.available = false;
        }
        this.changeDetectorRef.markForCheck();
      });
  }

  public copySystemName(): void {
    if (!this.systemCycleAnalysis) {
      return;
    }
    navigator.clipboard.writeText(this.systemCycleAnalysis.SystemName);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }


  public ngAfterViewInit(): void {
    this.contributions.sort = this.sort;
  }

  public getSourceAmount(source: string, row: Contribution): number | null {
    const sourceAmount = row.SourceAmounts.find(s => s.Source === source);
    if (sourceAmount) {
      return sourceAmount.Amount;
    }
    return null;
  }
}

interface OverwatchStarSystemCycleAnalysis {
  SystemAddress: number;
  SystemName: string;
  ProgressStart: string;
  ProgressCompleted: string;
  ThargoidState: OverwatchThargoidLevel;
  CycleWarEffortsUntilCompleted: OverwatchStarSystemCycleAnalysisWarEffort[];
}

interface OverwatchStarSystemCycleAnalysisWarEffort {
  Type: string;
  TypeId: number;
  Source: string;
  SourceId: number;
  TypeGroup: string;
  Amount: number,
}

interface Contribution {
  Type: string;
  SourceAmounts: {
    Source: string;
    Amount: number;
  }[];
}