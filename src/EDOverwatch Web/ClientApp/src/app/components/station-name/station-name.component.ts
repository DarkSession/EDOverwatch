import { Component, Input, OnChanges, ViewEncapsulation } from '@angular/core';

@Component({
  selector: 'app-station-name',
  templateUrl: './station-name.component.html',
  styleUrls: ['./station-name.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class StationNameComponent implements OnChanges {
  @Input() station!: OverwatchStation;
  public landingPads = "";

  public ngOnChanges(): void {
    const landingPads = ["Landing pads:"];
    if (this.station.LandingPads) {
      if (this.station.LandingPads.Large) {
        landingPads.push("Large: " + this.station.LandingPads.Large);
      }
      if (this.station.LandingPads.Medium) {
        landingPads.push("Medium: " + this.station.LandingPads.Medium);
      }
      if (this.station.LandingPads.Small) {
        landingPads.push("Small: " + this.station.LandingPads.Small);
      }
    }
    this.landingPads = landingPads.join(`\r\n`);
  }
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
  OdysseyOnly: boolean;
  RescueShip: OverwatchStationRescueShip | null;
  BodyName: string | null;
}

interface OverwatchStationRescueShip {
  Name: string;
  System: OverwatchStarSystemMin;
  DistanceLy: number;
}

export interface OverwatchStarSystemMin {
  SystemAddress: number;
  Name: string;
  Population: number;
}