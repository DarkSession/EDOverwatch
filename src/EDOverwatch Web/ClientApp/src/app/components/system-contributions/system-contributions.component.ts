import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { OverwatchStarSystemFullDetail } from '../system/system.component';

@Component({
  selector: 'app-system-contributions',
  templateUrl: './system-contributions.component.html',
  styleUrls: ['./system-contributions.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemContributionsComponent implements OnChanges, AfterViewInit {
  public contributions: MatTableDataSource<ContributionDetail> = new MatTableDataSource<ContributionDetail>();
  public warEffortsDetailsDisplayedColumns = ['Date', 'Type'];

  public sources: string[] = [];
  @ViewChild(MatSort) detailsSort!: MatSort;

  @Input() starSystem!: OverwatchStarSystemFullDetail;

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
    this.warEffortsDetailsDisplayedColumns = ['Date', 'Type', ...this.sources];

    const contributions: ContributionDetail[] = [];
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
    this.contributions = new MatTableDataSource<ContributionDetail>(contributions);
    this.contributions.sort = this.detailsSort;
    this.changeDetectorRef.markForCheck();
  }

  public ngAfterViewInit(): void {
    this.contributions.sort = this.detailsSort;
  }

  public getSourceAmount(source: string, row: ContributionDetail): number | null {
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

interface ContributionDetail extends Contribution {
  Date: string;
}
