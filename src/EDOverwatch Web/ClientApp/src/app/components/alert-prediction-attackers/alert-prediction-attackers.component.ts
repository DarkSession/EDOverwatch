import { Component, Input, OnChanges } from '@angular/core';
import { OverwatchStarSystem } from '../system-list/system-list.component';
import { faPeopleGroup } from '@fortawesome/pro-duotone-svg-icons';

@Component({
  selector: 'app-alert-prediction-attackers',
  templateUrl: './alert-prediction-attackers.component.html',
  styleUrls: ['./alert-prediction-attackers.component.css']
})
export class AlertPredictionAttackersComponent implements OnChanges {
  public readonly faPeopleGroup = faPeopleGroup;
  @Input() attackers?: OverwatchAlertPredictionSystemAttacker[];
  public visibleAttackers: OverwatchAlertPredictionSystemAttacker[] = [];
  public additionalEntries = 0;
  public limitVisibleAttackers = true;

  public ngOnChanges(): void {
    this.updateVisibleAttackers();
  }

  public updateVisibleAttackers(): void {
    if (!this.attackers) {
      return;
    }
    if (this.attackers.length <= 7 || !this.limitVisibleAttackers) {
      this.visibleAttackers = this.attackers;
      return;
    }
    let entry = 0;
    this.visibleAttackers = [];
    for (const attacker of this.attackers) {
      this.visibleAttackers.push(attacker);
      entry++;
      if (entry >= 5) {
        break;
      }
    }
    this.additionalEntries = this.attackers.length - this.visibleAttackers.length;
  }

  public toggleLimitVisibleAttackers(): void {
    this.limitVisibleAttackers = !this.limitVisibleAttackers;
    this.updateVisibleAttackers();
  }
}

export interface OverwatchAlertPredictionSystemAttacker {
  StarSystem: OverwatchStarSystem;
  Distance: number;
  IsActive: boolean;
}