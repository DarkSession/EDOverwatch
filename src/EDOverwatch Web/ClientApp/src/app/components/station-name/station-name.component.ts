import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-station-name',
  templateUrl: './station-name.component.html',
  styleUrls: ['./station-name.component.css']
})
export class StationNameComponent {
  @Input() station!: OverwatchStation;
}

export interface OverwatchStation {
  Name: string;
  MarketId: number;
  DistanceFromStarLS: number;
  Type: string;
  LandingPads: {
    Small: number;
    Medium: number;
    Large: number;
  }
  State: string;
  Gravity: number | null;
}