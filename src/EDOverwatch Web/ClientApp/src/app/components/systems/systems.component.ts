import { Component, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { OverwatchStarSystem, SystemListComponent } from '../system-list/system-list.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';

@UntilDestroy()
@Component({
  selector: 'app-systems',
  templateUrl: './systems.component.html',
  styleUrls: ['./systems.component.scss']
})
export class SystemsComponent implements OnInit {
  public dataRaw: OverwatchStarSystem[] = [];
  @ViewChild(SystemListComponent) systemList: SystemListComponent | null = null;

  public maelstroms: OverwatchMaelstrom[] = [];
  public maelstromsSelected: OverwatchMaelstrom[] = [];

  public thargoidLevels: OverwatchThargoidLevel[] = [];
  public thargoidLevelsSelected: OverwatchThargoidLevel[] = [];

  public constructor(
    private readonly webSocketService: WebsocketService) {
  }

  public ngOnInit(): void {
    this.loadSystems();
    this.webSocketService
      .on<OverwatchSystems>("OverwatchSystems")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.processOverwatchSystemsResponse(message.Data);
      });
  }

  private async loadSystems(): Promise<void> {
    const response = await this.webSocketService.sendMessageAndWaitForResponse<OverwatchSystems>("OverwatchSystems", {});
    if (response && response.Success) {
      this.processOverwatchSystemsResponse(response.Data);
    }
  }

  private processOverwatchSystemsResponse(data: OverwatchSystems): void {
    this.dataRaw = data.Systems;
    this.maelstroms = data.Maelstroms;
    this.maelstromsSelected = data.Maelstroms;
    this.thargoidLevels = data.Levels;
    this.thargoidLevelsSelected = data.Levels;
  }

  public updateDataSource(): void {
    if (this.systemList) {
      this.systemList.updateDataSource();
    }
  }
}

interface OverwatchSystems {
  Maelstroms: OverwatchMaelstrom[];
  Levels: OverwatchThargoidLevel[];
  Systems: OverwatchStarSystem[];
}

