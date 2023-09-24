import { ChangeDetectionStrategy, Component, Input, OnChanges } from '@angular/core';
import { CommanderWarEffortCycleStarSystemWarEffort, WarEffortTypeGroup } from '../my-efforts/my-efforts.component';
import { IconDefinition, faTruckRampBox } from '@fortawesome/pro-duotone-svg-icons';
import { faCrosshairs, faHandshake } from '@fortawesome/pro-light-svg-icons';
import { faTruck, faKitMedical } from '@fortawesome/pro-duotone-svg-icons';

@Component({
  selector: 'app-my-efforts-cycle-system-effort-type',
  templateUrl: './my-efforts-cycle-system-effort-type.component.html',
  styleUrls: ['./my-efforts-cycle-system-effort-type.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyEffortsCycleSystemEffortTypeComponent implements OnChanges {
  public icon: IconDefinition = faHandshake;
  @Input() effort: CommanderWarEffortCycleStarSystemWarEffort | null = null;

  public ngOnChanges(): void {
    switch (this.effort?.Group) {
      case WarEffortTypeGroup.Kills: {
        this.icon = faCrosshairs;
        break;
      }
      case WarEffortTypeGroup.Rescue: {
        this.icon = faKitMedical;
        break;
      }
      case WarEffortTypeGroup.Supply: {
        this.icon = faTruck;
        break;
      }
      case WarEffortTypeGroup.Mission: {
        break;
      }
      case WarEffortTypeGroup.RecoveryAndProbing: {
        this.icon = faTruckRampBox;
        break;
      }
      default: {
        this.icon = faHandshake;
        break;
      }
    }
  }
}
