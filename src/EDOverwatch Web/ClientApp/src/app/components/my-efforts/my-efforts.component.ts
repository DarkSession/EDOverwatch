import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchStarSystem } from '../system-list/system-list.component';
import { faAngleLeft, faAngleRight } from '@fortawesome/pro-light-svg-icons';
import { OverwatchThargoidCycle } from '../home-v2/home-v2.component';

@UntilDestroy()
@Component({
  selector: 'app-my-efforts',
  templateUrl: './my-efforts.component.html',
  styleUrls: ['./my-efforts.component.scss']
})
export class MyEffortsComponent implements OnInit {
  public readonly faAngleRight = faAngleRight;
  public readonly faAngleLeft = faAngleLeft;
  public readonly displayedColumns = ['Date', 'SystemName', 'Type', 'Amount'];
  public data: CommanderWarEffortsV3 | null = null;
  public date: string | null = null;
  public cycleData: CommandWarEffortCycle | null | undefined = null;

  public constructor(
    private readonly webSocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.webSocketService
      .on<CommanderWarEffortsV3>("CommanderWarEffortsV3")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.updateEfforts(message.Data);
      });
    this.webSocketService.sendMessage("CommanderWarEffortsV3", {});
  }

  private updateEfforts(data: CommanderWarEffortsV3): void {
    this.data = data;
    if (!this.date) {
      this.date = data.ThargoidCycles.find(t => t.IsCurrent)?.Cycle ?? null;
    }
    this.dateChanged();
    this.changeDetectorRef.detectChanges();
  }

  public dateChanged(): void {
    this.cycleData = this.data?.CycleWarEfforts.find(t => t.ThargoidCycle.Cycle == this.date);
  }

  private getCycleIndex(): number {
    const currentCycleIndex = this.data?.ThargoidCycles.findIndex(t => t.Cycle == this.date) ?? -1;
    return currentCycleIndex;
  }

  public previousCycle(): void {
    if (!this.data) {
      return;
    }
    const currentCycleIndex = this.getCycleIndex();
    if (currentCycleIndex === -1) {
      return;
    }
    if (currentCycleIndex > 0) {
      this.date = this.data.ThargoidCycles[(currentCycleIndex - 1)].Cycle;
      this.dateChanged();
    }
  }

  public nextCycle(): void {
    if (!this.data) {
      return;
    }
    const currentCycleIndex = this.getCycleIndex();
    if (currentCycleIndex === -1) {
      return;
    }
    if (currentCycleIndex < (this.data.ThargoidCycles.length - 1)) {
      this.date = this.data.ThargoidCycles[(currentCycleIndex + 1)].Cycle;
      this.dateChanged();
    }
  }
}

interface CommanderWarEffortsV3 {
  CycleWarEfforts: CommandWarEffortCycle[];
  WarEffortTypeGroups: string[];
  ThargoidCycles: OverwatchThargoidCycle[];
}

export interface CommandWarEffortCycle {
  ThargoidCycle: OverwatchThargoidCycle;
  StarSystems: CommanderWarEffortCycleStarSystem[];
}

export interface CommanderWarEffortCycleStarSystem {
  StarSystem: OverwatchStarSystem;
  WarEfforts: CommanderWarEffortCycleStarSystemWarEffort[];
}

export interface CommanderWarEffortCycleStarSystemWarEffort {
  Date: string;
  Type: string;
  Group: WarEffortTypeGroup;
  Amount: number;
}

export enum WarEffortTypeGroup {
  Kills,
  Rescue,
  Supply,
  Mission,
  RecoveryAndProbing,
}