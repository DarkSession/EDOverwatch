import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { OverwatchStarSystemDetail, OverwatchStarSystemWarEffortCycle } from '../system/system.component';

@Component({
  selector: 'app-system-contribution-summary',
  templateUrl: './system-contribution-summary.component.html',
  styleUrls: ['./system-contribution-summary.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemContributionSummaryComponent implements OnChanges, AfterViewInit {
  @ViewChild('summarySort') summarySort!: MatSort;
  @Input() starSystem!: OverwatchStarSystemDetail;
  @Input() summary!: OverwatchStarSystemWarEffortCycle;
  public warEffortsSummeriesDisplayedColumns = ['Type'];
  public contributions: MatTableDataSource<Contribution> = new MatTableDataSource<Contribution>();

  public sources: string[] = [];
  @ViewChild(MatSort) sort!: MatSort;

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnChanges(): void {
    const sourcesWithData = this.starSystem.WarEfforts.map(w => w.Source);
    this.sources = [];
    for (const source of this.starSystem.WarEffortSources) {
      if (sourcesWithData.includes(source.Name)) {
        this.sources.push(source.Name);
      }
    }
    this.warEffortsSummeriesDisplayedColumns = ['Type', ...this.sources];

    const contributions: Contribution[] = [];
    for (const effortTotal of this.summary.EffortTotals) {
      let contribution = contributions.find(d => d.Type === effortTotal.Type);
      if (!contribution) {
        const sourceAmounts = this.sources.map((s) => {
          return { Source: s, Amount: 0 }
        });
        contribution = {
          Type: effortTotal.Type,
          SourceAmounts: sourceAmounts,
        };
        contributions.push(contribution);
      }
      const sourceAmount = contribution.SourceAmounts.find(s => s.Source === effortTotal.Source);
      if (sourceAmount) {
        sourceAmount.Amount += effortTotal.Amount;
      }
    }
    this.contributions = new MatTableDataSource<Contribution>(contributions);
    this.contributions.sort = this.sort;
    this.changeDetectorRef.markForCheck();
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

interface Contribution {
  Type: string;
  SourceAmounts: {
    Source: string;
    Amount: number;
  }[];
}
