import { AfterViewInit, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { OverwatchAlertPredictionMaelstrom, OverwatchAlertPredictionMaelstromAttackerCount } from '../alert-prediction/alert-prediction.component';
import { MatSnackBar } from '@angular/material/snack-bar';
import { faClipboard } from '@fortawesome/pro-light-svg-icons';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';

@Component({
  selector: 'app-alert-prediction-top-attackers',
  templateUrl: './alert-prediction-top-attackers.component.html',
  styleUrls: ['./alert-prediction-top-attackers.component.css']
})
export class AlertPredictionTopAttackersComponent implements OnChanges, AfterViewInit {
  public readonly faClipboard = faClipboard;
  @ViewChild(MatSort) sort!: MatSort;
  @Input() alertPrediction: OverwatchAlertPredictionMaelstrom | null = null;
  public data: MatTableDataSource<OverwatchAlertPredictionMaelstromAttackerCount> = new MatTableDataSource<OverwatchAlertPredictionMaelstromAttackerCount>();
  public readonly columns = ["Name", "Distance", "PossibleAttacks"];

  public constructor(private readonly matSnackBar: MatSnackBar) {
  }

  public ngOnChanges(): void {
    if (!this.alertPrediction) {
      return;
    }
    this.data = new MatTableDataSource<OverwatchAlertPredictionMaelstromAttackerCount>(this.alertPrediction.AttackingSystemCount);
    this.data.sort = this.sort;
    this.data.sortingDataAccessor = (system: OverwatchAlertPredictionMaelstromAttackerCount, columnName: string): string | number => {
      switch (columnName) {
        case "Name": {
          return system.StarSystem.Name;
        }
        case "Distance": {
          return system.StarSystem.DistanceToMaelstrom;
        }
        case "PossibleAttacks": {
          return system.Count;
        }
      }
      return "";
    }
  }

  public ngAfterViewInit(): void {
    this.ngOnChanges();
  }

  public copySystemName(starSystem: OverwatchAlertPredictionMaelstromAttackerCount): void {
    navigator.clipboard.writeText(starSystem.StarSystem.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }
}
