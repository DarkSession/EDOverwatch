import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-maelstrom-name',
  templateUrl: './maelstrom-name.component.html',
  styleUrls: ['./maelstrom-name.component.css']
})
export class MaelstromNameComponent {
  @Input() maelstrom!: OverwatchMaelstrom;
  @Input() titanPrefix = false;
}

export interface OverwatchMaelstrom {
  Name: string;
  SystemName: string;
  SystemAddress: number;
  IngameNumber: number;
}

export interface OverwatchMaelstromProgress extends OverwatchMaelstrom {
  HeartsRemaining: number;
  HeartProgress: number;
  TotalProgress: number;
  State: string;
  MeltdownTimeEstimate: string | null;
}