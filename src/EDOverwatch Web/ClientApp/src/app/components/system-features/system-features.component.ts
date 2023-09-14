import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges } from '@angular/core';
import { OverwatchStarSystemFull } from '../system-list/system-list.component';
import { faBolt } from '@fortawesome/free-solid-svg-icons';
import { faCrosshairs, faCrosshairsSimple } from '@fortawesome/pro-light-svg-icons';

@Component({
  selector: 'app-system-features',
  templateUrl: './system-features.component.html',
  styleUrls: ['./system-features.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemFeaturesComponent implements OnChanges {
  public readonly faBolt = faBolt;
  public readonly faCrosshairs = faCrosshairs;
  public readonly faCrosshairsSimple = faCrosshairsSimple;
  @Input() starSystem: OverwatchStarSystemFull | null = null;
  public barnacleMatrix = false;
  public odysseySettlement = false;
  public federation = false;
  public empire = false;
  public thargoidControlledReactivationMissions = false;
  public aXConflictZones = false;
  public groundPortAXCZ = false;

  public constructor(private readonly changeDetectorRef: ChangeDetectorRef) {
  }

  public ngOnChanges(): void {
    this.barnacleMatrix = this.starSystem?.Features?.includes("BarnacleMatrix") ?? false;
    this.odysseySettlement = this.starSystem?.Features?.includes("OdysseySettlements") ?? false;
    this.federation = this.starSystem?.Features?.includes("FederalFaction") ?? false;
    this.empire = this.starSystem?.Features?.includes("ImperialFaction") ?? false;
    this.thargoidControlledReactivationMissions = this.starSystem?.Features?.includes("ThargoidControlledReactivationMissions") ?? false;
    this.aXConflictZones = this.starSystem?.Features?.includes("AXConflictZones") ?? false;
    this.groundPortAXCZ = this.starSystem?.Features?.includes("GroundPortAXCZ") ?? false;
    this.changeDetectorRef.markForCheck();
  }
}
