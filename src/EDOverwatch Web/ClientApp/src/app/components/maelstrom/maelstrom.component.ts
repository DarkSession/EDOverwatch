import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';

@UntilDestroy()
@Component({
  selector: 'app-maelstrom',
  templateUrl: './maelstrom.component.html',
  styleUrls: ['./maelstrom.component.css']
})
export class MaelstromComponent implements OnInit {
  public maelstrom: OverwatchMaelstrom | null = null;

  public constructor(
    private readonly route: ActivatedRoute,
    private readonly websocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.route.paramMap
    .pipe(untilDestroyed(this))
    .subscribe((p: ParamMap) => {
      const name = p.get("name");
      if (name && this.maelstrom?.Name != name) {
        this.requestMaelstrom(name);
      }
    });
  }

  private async requestMaelstrom(name: string): Promise<void> {

  }
}
