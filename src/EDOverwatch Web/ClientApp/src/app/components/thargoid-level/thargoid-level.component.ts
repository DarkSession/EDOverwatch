import { Component, Input, OnChanges } from '@angular/core';
import { faEyeLowVision } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-thargoid-level',
  templateUrl: './thargoid-level.component.html',
  styleUrls: ['./thargoid-level.component.css']
})
export class ThargoidLevelComponent implements OnChanges {
  public readonly faEyeLowVision = faEyeLowVision;
  @Input() thargoidLevel!: OverwatchThargoidLevel;

  public dotClass = "";

  public ngOnChanges(): void {
    switch (this.thargoidLevel?.Level) {
      case StarSystemThargoidLevelState.Alert: {
        this.dotClass = "thargoid-alert";
        break;
      }
      case StarSystemThargoidLevelState.Invasion: {
        this.dotClass = "thargoid-invasion";
        break;
      }
      case StarSystemThargoidLevelState.Controlled: {
        this.dotClass = "thargoid-controlled";
        break;
      }
      case StarSystemThargoidLevelState.Maelstrom: {
        this.dotClass = "thargoid-maelstrom";
        break;
      }
      case StarSystemThargoidLevelState.Recovery: {
        this.dotClass = "thargoid-recovery";
        break;
      }
    }
  }
}

export interface OverwatchThargoidLevel {
  Level: number;
  Name: string;
  IsInvisibleState: boolean;
}

enum StarSystemThargoidLevelState {
  None = 0,
  Alert = 20,
  Invasion = 30,
  Controlled = 40,
  Maelstrom = 50,
  Recovery = 70,
}