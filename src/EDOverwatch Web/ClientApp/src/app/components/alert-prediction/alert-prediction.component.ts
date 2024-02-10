import { AfterViewInit, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { OverwatchStarSystemMin } from '../station-name/station-name.component';
import { OverwatchAlertPredictionSystemAttacker } from '../alert-prediction-attackers/alert-prediction-attackers.component';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { faBullseyeArrow } from '@fortawesome/pro-duotone-svg-icons';
import { faClipboard } from '@fortawesome/pro-light-svg-icons';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchStarSystem } from '../system-list/system-list.component';

@Component({
  selector: 'app-alert-prediction',
  templateUrl: './alert-prediction.component.html',
  styleUrls: ['./alert-prediction.component.scss']
})
export class AlertPredictionComponent implements OnChanges, AfterViewInit {
  public readonly faClipboard = faClipboard;
  public readonly faBullseyeArrow = faBullseyeArrow;
  @Input() alertPrediction: OverwatchAlertPredictionMaelstrom | null = null;
  public sortedAlertPredictions: MatTableDataSource<OverwatchAlertPredictionSystem> = new MatTableDataSource<OverwatchAlertPredictionSystem>();
  public readonly alertPredictionColumns = ['Name', 'Population', 'Distance', 'Attackers'];
  @ViewChild(MatSort, { static: false }) sort!: MatSort;
  public showAll = false;

  public constructor(private readonly matSnackBar: MatSnackBar) {
  }

  public ngOnChanges(): void {
    if (!this.alertPrediction) {
      return;
    }
    const alertPredictions = (this.showAll || this.alertPrediction.Systems.length < 12) ? this.alertPrediction.Systems : [...this.alertPrediction.Systems].splice(0, 10);
    this.sortedAlertPredictions = new MatTableDataSource<OverwatchAlertPredictionSystem>(alertPredictions);
    this.sortedAlertPredictions.sort = this.sort;
    this.sortedAlertPredictions.sortingDataAccessor = (system: OverwatchAlertPredictionSystem, columnName: string): string | number => {
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
  }

  public ngAfterViewInit(): void {
    this.ngOnChanges();
  }

  public copySystemName(starSystem: OverwatchAlertPredictionSystem): void {
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

export interface OverwatchAlertPredictionSystem {
  StarSystem: OverwatchStarSystemMin;
  Distance: number;
  Attackers: OverwatchAlertPredictionSystemAttacker[];
  PrimaryTarget: boolean;
  IsActive: boolean;
  Order: number;
  SpireSite: boolean;
}


export interface OverwatchAlertPredictionMaelstrom {
  Maelstrom: OverwatchMaelstrom;
  Systems: OverwatchAlertPredictionSystem[];
  AttackingSystemCount: OverwatchAlertPredictionMaelstromAttackerCount[];
  ExpectedAlerts: number;
}

export interface OverwatchAlertPredictionMaelstromAttackerCount {
  StarSystem: OverwatchStarSystem;
  Count: number;
}