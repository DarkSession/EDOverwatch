import { AfterViewInit, ChangeDetectorRef, Component, Input, OnChanges, OnInit, ViewChild } from '@angular/core';
import { OverwatchStarSystemFullDetail, OverwatchStarSystemNearbySystem } from '../system/system.component';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { faClipboard } from '@fortawesome/pro-light-svg-icons';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AppService } from 'src/app/services/app.service';

@Component({
  selector: 'app-system-attack-defense',
  templateUrl: './system-attack-defense.component.html',
  styleUrls: ['./system-attack-defense.component.css']
})
export class SystemAttackDefenseComponent implements OnChanges, AfterViewInit, OnInit {
  public readonly faClipboard = faClipboard;
  @Input() starSystem!: OverwatchStarSystemFullDetail;
  @ViewChild(MatSort) sort!: MatSort;
  public nearbySystems: MatTableDataSource<OverwatchStarSystemNearbySystem> = new MatTableDataSource<OverwatchStarSystemNearbySystem>();
  public nearbySystemDisplayedColumns = ["Name", "ThargoidLevel", "Distance"];
  public warEffortEstimates: MatTableDataSource<WarEffortEstimateRow> = new MatTableDataSource<WarEffortEstimateRow>();
  public warEffortEstimatesColumns = ["Type", "Amount"];
  public showEffortEstimates = false;

  public constructor(private readonly matSnackBar: MatSnackBar,
    private readonly appService: AppService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.updateSettings();
  }

  private async updateSettings(): Promise<void> {
    this.showEffortEstimates = (await this.appService.getSetting("ExperimentalEffortEstimates")) === "1";
    this.changeDetectorRef.detectChanges();
  }

  public ngOnChanges(): void {
    this.nearbySystems = new MatTableDataSource<OverwatchStarSystemNearbySystem>(this.starSystem.NearbySystems);
    this.nearbySystems.sortingDataAccessor = (s: OverwatchStarSystemNearbySystem, columnName: string): string | number => {
      switch (columnName) {
        case "Name": {
          return s.StarSystem.Name;
        }
        case "Distance": {
          return s.Distance;
        }
        default: {
          return "";
        }
      }
    }
    this.nearbySystems.sort = this.sort;
    const estimates: WarEffortEstimateRow[] = [];
    if (this.starSystem.AttackDefense)  {
      estimates.push({
        type: "Scout/interceptor tissue samples (remaining)",
        amount: this.starSystem.AttackDefense.RequirementsTissueSampleRemaining,
      });
      estimates.push({
        type: "Scout/interceptor tissue samples (total)",
        amount: this.starSystem.AttackDefense.RequirementsTissueSampleTotal,
      });
    }
    this.warEffortEstimates = new MatTableDataSource<WarEffortEstimateRow>(estimates);
    console.log(this.warEffortEstimates);
  }

  public ngAfterViewInit(): void {
    this.nearbySystems.sort = this.sort;
  }

  public copySystemName(row: OverwatchStarSystemNearbySystem): void {
    navigator.clipboard.writeText(row.StarSystem.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }
}

interface WarEffortEstimateRow {
  type: string;
  amount: number | null;
}