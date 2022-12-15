import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';

@UntilDestroy()
@Component({
  selector: 'app-systems',
  templateUrl: './systems.component.html',
  styleUrls: ['./systems.component.scss']
})
export class SystemsComponent implements OnInit {
  public readonly faClipboard = faClipboard;
  public readonly displayedColumns = ['Name', 'ThargoidLevel', 'Maelstrom', 'Progress', 'EffortFocus'];
  private dataRaw: OverwatchStarSystem[] = [];

  public dataSource: MatTableDataSource<OverwatchStarSystem> = new MatTableDataSource<OverwatchStarSystem>();

  public maelstroms: OverwatchMaelstrom[] = [];
  public maelstromsSelected: OverwatchMaelstrom[] = [];

  public thargoidLevels: OverwatchThargoidLevel[] = [];
  public thargoidLevelsSelected: OverwatchThargoidLevel[] = [];

  @ViewChild(MatSort, { static: true }) sort!: MatSort;

  public constructor(
    private readonly webSocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar) {
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
    this.updateDataSource();
  }

  public copySystemName(starSystem: OverwatchStarSystem): void {
    navigator.clipboard.writeText(starSystem.Name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }

  public updateDataSource(): void {
    let data = this.dataRaw.filter(d =>
      typeof this.maelstromsSelected.find(m => m.Name === d.Maelstrom.Name) !== 'undefined' &&
      typeof this.thargoidLevelsSelected.find(t => t.Level === d.ThargoidLevel.Level) !== 'undefined');
    if (!this.sort?.active) {
      this.dataSource.data.sort((system1: OverwatchStarSystem, system2: OverwatchStarSystem) => {
        if (system1.ThargoidLevel.Level > system2.ThargoidLevel.Level) {
          return -1;
        }
        else if (system1.ThargoidLevel.Level < system2.ThargoidLevel.Level) {
          return 1;
        }
        return system1.Name.localeCompare(system2.Name);
      });
    }
    else {
      data = this.dataSource.sortData(data, this.sort);
    }
    this.dataSource = new MatTableDataSource<OverwatchStarSystem>(data);
    this.dataSource.sort = this.sort;
    this.dataSource.sortingDataAccessor = (system: OverwatchStarSystem, columnName: string): string => {
      switch (columnName) {
        case "ThargoidLevel": {
          return system.ThargoidLevel.Name;
        }
        case "Maelstrom": {
          return system.Maelstrom.Name;
        }
      }
      return system[columnName as keyof OverwatchStarSystem] as string;
    }
  
    this.changeDetectorRef.detectChanges();
  }
}

interface OverwatchSystems {
  Maelstroms: OverwatchMaelstrom[];
  Levels: OverwatchThargoidLevel[];
  Systems: OverwatchStarSystem[];
}

interface OverwatchThargoidLevel {
  Level: number;
  Name: string;
}

export interface OverwatchStarSystem {
  SystemAddress: number;
  Name: string;
  Maelstrom: OverwatchMaelstrom;
  ThargoidLevel: OverwatchThargoidLevel;
  Progress: number | null;
  EffortFocus: number;
}
