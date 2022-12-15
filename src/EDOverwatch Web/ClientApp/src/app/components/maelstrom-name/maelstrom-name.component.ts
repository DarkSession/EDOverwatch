import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-maelstrom-name',
  templateUrl: './maelstrom-name.component.html',
  styleUrls: ['./maelstrom-name.component.css']
})
export class MaelstromNameComponent {
  @Input() maelstrom!: OverwatchMaelstrom;
}

export interface OverwatchMaelstrom {
  Name: string;
  SystemName: string;
}