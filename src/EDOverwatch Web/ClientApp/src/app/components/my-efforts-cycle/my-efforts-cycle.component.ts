import { Component, Input } from '@angular/core';
import { CommandWarEffortCycle } from '../my-efforts/my-efforts.component';

@Component({
  selector: 'app-my-efforts-cycle',
  templateUrl: './my-efforts-cycle.component.html',
  styleUrls: ['./my-efforts-cycle.component.scss']
})
export class MyEffortsCycleComponent {
  @Input() cycleWarEffort: CommandWarEffortCycle | null = null;

  public constructor() {
  }
}
