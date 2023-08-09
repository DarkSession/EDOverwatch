import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchAlertPredictionSystem } from '../alert-prediction/alert-prediction.component';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchThargoidCycle } from '../home/home.component';

@UntilDestroy()
@Component({
  selector: 'app-alert-prediction-overview',
  templateUrl: './alert-prediction-overview.component.html',
  styleUrls: ['./alert-prediction-overview.component.css']
})
export class AlertPredictionOverviewComponent implements OnInit {
  public predictions: OverwatchAlertPredictions | null = null; 

  public constructor(
    private readonly webSocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef) {
  }

  public ngOnInit(): void {
    this.webSocketService
      .on<OverwatchAlertPredictions>("OverwatchAlertPredictions")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.predictions = message.Data;
        this.changeDetectorRef.detectChanges();
      });
    this.webSocketService
      .onReady
      .pipe(untilDestroyed(this))
      .subscribe((isReconnect: boolean) => {
        if (isReconnect) {
          this.webSocketService.sendMessage("OverwatchAlertPredictions", {});
        }
      });
    this.webSocketService.sendMessage("OverwatchAlertPredictions", {});
  }
}

interface OverwatchAlertPredictions {
  Maelstroms: OverwatchAlertPredictionMaelstrom[];
}

interface OverwatchAlertPredictionMaelstrom {
  Maelstrom: OverwatchMaelstrom;
  Systems: OverwatchAlertPredictionSystem[];
  Cycle: OverwatchThargoidCycle;
}