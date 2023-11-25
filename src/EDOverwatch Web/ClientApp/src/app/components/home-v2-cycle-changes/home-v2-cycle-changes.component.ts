import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-home-v2-cycle-changes',
  templateUrl: './home-v2-cycle-changes.component.html',
  styleUrls: ['./home-v2-cycle-changes.component.scss']
})
export class HomeV2CycleChangesComponent {
  @Input() cycleChange: OverwatchOverviewV2CycleChange | null | undefined = null;
  @Input() title = "";
  @Input() future = false;
}

export interface OverwatchOverviewV2CycleChange {
  AlertsDefended: number;
  InvasionsDefended: number;
  ControlsDefended: number;
  TitansDefeated: number;
  ThargoidInvasionStarted: number;
  ThargoidsGained: number;
}