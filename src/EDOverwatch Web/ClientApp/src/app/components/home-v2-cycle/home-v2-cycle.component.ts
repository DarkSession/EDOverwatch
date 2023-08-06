import { Component, Input } from '@angular/core';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';

@Component({
  selector: 'app-home-v2-cycle',
  templateUrl: './home-v2-cycle.component.html',
  styleUrls: ['./home-v2-cycle.component.scss']
})
export class HomeV2CycleComponent {
  @Input() cycle: OverwatchOverviewV2Cycle | null | undefined = null;
  @Input() title: string = "";
  @Input() information: string | null = null;
  public svgWidth = 0;
  public stateAlert: OverwatchThargoidLevel = {
    Level: 20,
    Name: "Alert",
  };
  public stateInvasion: OverwatchThargoidLevel = {
    Level: 30,
    Name: "Invasion",
  };
  public stateControlled: OverwatchThargoidLevel = {
    Level: 40,
    Name: "Controlled",
  };
  public stateTitan: OverwatchThargoidLevel = {
    Level: 50,
    Name: "Titan",
  };
  public stateRecovery: OverwatchThargoidLevel = {
    Level: 70,
    Name: "Recovery",
  };
}

export interface OverwatchOverviewV2Cycle {
  CycleStart: string;
  CycleEnd: string;
  Alerts: number;
  Invasions: number;
  Controls: number;
  Titans: number;
  Recovery: number;
}