import { Component, ViewChild } from '@angular/core';

@Component({
  selector: 'app-map-historical',
  templateUrl: './map-historical.component.html',
  styleUrls: ['./map-historical.component.css']
})
export class MapHistoricalComponent {
  @ViewChild('container') container: any;
}
