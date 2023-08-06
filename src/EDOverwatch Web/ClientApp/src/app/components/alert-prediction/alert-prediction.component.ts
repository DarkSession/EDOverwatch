import { Component, Input, OnChanges, ViewChild } from '@angular/core';
import { OverwatchStarSystemMin } from '../station-name/station-name.component';
import { OverwatchMaelstromDetailAlertPredictionAttacker } from '../alert-prediction-attackers/alert-prediction-attackers.component';
import { MatSnackBar } from '@angular/material/snack-bar';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { faBullseye } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-alert-prediction',
  templateUrl: './alert-prediction.component.html',
  styleUrls: ['./alert-prediction.component.css']
})
export class AlertPredictionComponent implements OnChanges {
  public readonly faClipboard = faClipboard;
  public readonly faBullseye = faBullseye;
  @Input() alertPredictions: OverwatchMaelstromDetailAlertPrediction[] = [];
  public sortedAlertPredictions: MatTableDataSource<OverwatchMaelstromDetailAlertPrediction> = new MatTableDataSource<OverwatchMaelstromDetailAlertPrediction>();
  public readonly alertPredictionColumns = ['Name', 'Population', 'Distance', 'Attackers'];
  @ViewChild(MatSort, { static: false }) sort!: MatSort;
  public showAll = false;

  public constructor(private readonly matSnackBar: MatSnackBar) {
  }
  
  public ngOnChanges(): void {
    const alertPredictions = (this.showAll || this.alertPredictions.length < 12) ? this.alertPredictions : this.alertPredictions.splice(0, 10);

    this.sortedAlertPredictions = new MatTableDataSource<OverwatchMaelstromDetailAlertPrediction>(alertPredictions);
    this.sortedAlertPredictions.sortingDataAccessor = (system: OverwatchMaelstromDetailAlertPrediction, columnName: string): string | number => {
      switch (columnName) {
        case "Name": {
          return system.StarSystem.Name;
        }
        case "Population": {
          return system.StarSystem.Population;
        }
        case "Distance": {
          return system.Distance;
        }
      }
      return "";
    }
    this.sortedAlertPredictions.sort = this.sort;
  }

  public copySystemName(starSystem: OverwatchMaelstromDetailAlertPrediction): void {
    navigator.clipboard.writeText(starSystem.StarSystem.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }

  public toggleShowAll(): void {
    this.showAll = !this.showAll;
    this.ngOnChanges();
  }
}

export interface OverwatchMaelstromDetailAlertPrediction {
  StarSystem: OverwatchStarSystemMin;
  Distance: number;
  Attackers: OverwatchMaelstromDetailAlertPredictionAttacker[];
  PrimaryTarget: boolean;
}
