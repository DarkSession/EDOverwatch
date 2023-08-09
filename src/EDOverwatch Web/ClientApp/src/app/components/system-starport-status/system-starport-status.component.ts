import { Component, Input } from '@angular/core';
import { OverwatchStarSystemFull } from '../system-list/system-list.component';

@Component({
  selector: 'app-system-starport-status',
  templateUrl: './system-starport-status.component.html',
  styleUrls: ['./system-starport-status.component.css']
})
export class SystemStarportStatusComponent {
  @Input() starSystem!: OverwatchStarSystemFull;
}
