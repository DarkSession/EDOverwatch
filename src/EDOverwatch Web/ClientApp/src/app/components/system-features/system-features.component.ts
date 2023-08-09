import { Component, Input, OnChanges } from '@angular/core';
import { OverwatchStarSystemFull } from '../system-list/system-list.component';

@Component({
  selector: 'app-system-features',
  templateUrl: './system-features.component.html',
  styleUrls: ['./system-features.component.css']
})
export class SystemFeaturesComponent implements OnChanges {
  @Input() starSystem: OverwatchStarSystemFull | null = null;
  public barnacleMatrix = false;
  public odysseySettlement = false;
  public federation = false;
  public empire = false;

  public ngOnChanges(): void {
    this.barnacleMatrix = this.starSystem?.Features?.includes("BarnacleMatrix") ?? false;
    this.odysseySettlement = this.starSystem?.Features?.includes("OdysseySettlements") ?? false;
    this.federation = this.starSystem?.Features?.includes("FederalFaction") ?? false;
    this.empire = this.starSystem?.Features?.includes("ImperialFaction") ?? false;
  }
}
