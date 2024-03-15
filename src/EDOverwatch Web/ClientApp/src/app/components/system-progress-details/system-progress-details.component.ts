import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { ProgressDetails } from '../system/system.component';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { AppService } from 'src/app/services/app.service';
import { faFileCsv } from '@fortawesome/pro-duotone-svg-icons';
import { download, generateCsv, mkConfig } from 'export-to-csv';

@Component({
  selector: 'app-system-progress-details',
  templateUrl: './system-progress-details.component.html',
  styleUrl: './system-progress-details.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemProgressDetailsComponent implements OnChanges, AfterViewInit {
  @ViewChild(MatPaginator) paginator: MatPaginator | null = null;
  public readonly faFileCsv = faFileCsv;

  @Input() progressData: ProgressDetails[] | null = null;
  public progressDetails: MatTableDataSource<ProgressDetails> = new MatTableDataSource<ProgressDetails>();
  public progressDetailsColumns = ["State", "Time", "Progress", "Change", "Timespan"];
  public progressDetailsPageSize: number = 25;

  public constructor(
    private readonly appService: AppService,
    private readonly changeDetectorRef: ChangeDetectorRef) {
  }

  public ngOnChanges(): void {
    if (this.progressData) {
      this.progressDetails = new MatTableDataSource<ProgressDetails>(this.progressData);
      this.progressDetails.paginator = this.paginator;
    }
  }

  public ngAfterViewInit() {
    if (this.progressDetails) {
      this.progressDetails.paginator = this.paginator;
      this.changeDetectorRef.markForCheck();
    }
  }

  public async handlePageEvent(e: PageEvent): Promise<void> {
    if (e.pageSize) {
      this.progressDetailsPageSize = e.pageSize;
      this.appService.saveSetting("SystemListPageSize", e.pageSize.toString());
    }
  }

  public exportToCsv(): void {
    if (!this.progressData) {
      return;
    }
    const data = [];
    for (const progressData of this.progressData) {
      data.push({
        State: progressData.State.Name,
        DateTime: progressData.DateTime,
        Progress: progressData.ProgressPercentage
      });
    }

    const csvConfig = mkConfig({
      fieldSeparator: ',',
      quoteStrings: true,
      decimalSeparator: '.',
      showTitle: false,
      filename: "Overwatch System Progress Details Export",
      useTextFile: false,
      useBom: true,
      useKeysAsHeaders: true,
    });

    const csv = generateCsv(csvConfig)(data);
    download(csvConfig)(csv);
  }
}
