import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { OverwatchStarSystemDetail } from '../system/system.component';

@Component({
  selector: 'app-system-contributions',
  templateUrl: './system-contributions.component.html',
  styleUrls: ['./system-contributions.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemContributionsComponent implements OnChanges, AfterViewInit {
  public contributions: MatTableDataSource<Contribution> = new MatTableDataSource<Contribution>();
  public warEffortsDisplayedColumns = ['Date', 'Type'];
  public sources: string[] = [];
  @ViewChild(MatSort) sort!: MatSort;

  @Input() starSystem!: OverwatchStarSystemDetail;

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
    this.warEffortsDisplayedColumns = ['Date', 'Type', ...this.sources];

    const contributions: Contribution[] = [];
    for (const warEffort of this.starSystem.WarEfforts) {
      let contribution = contributions.find(c => c.Date === warEffort.Date && c.Type === warEffort.Type);
      if (!contribution) {
        const sourceAmounts = this.sources.map((s) => {
          return { Source: s, Amount: 0 }
        });
        contribution = {
          Date: warEffort.Date,
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
  Date: string;
  Type: string;
  SourceAmounts: {
    Source: string;
    Amount: number;
  }[];
}