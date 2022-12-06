import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-systems',
  templateUrl: './systems.component.html',
  styleUrls: ['./systems.component.scss']
})
export class SystemsComponent implements OnInit {
  public readonly faClipboard = faClipboard;
  public readonly displayedColumns = ['systemName', 'thargoidLevel', 'maelstrom'];
  private dataRaw: OverwatchStarSystem[] = [];

  public dataSource: OverwatchStarSystem[] = [];

  public maelstroms: OverwatchMaelstrom[] = [];
  public maelstromsSelected: OverwatchMaelstrom[] = [];

  public thargoidLevels: OverwatchThargoidLevel[] = [];
  public thargoidLevelsSelected: OverwatchThargoidLevel[] = [];

  public constructor(
    private readonly httpClient: HttpClient,
    @Inject('BASE_URL') private readonly baseUrl: string,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar) {
  }

  public ngOnInit(): void {
    this.loadSystems();
  }

  private async loadSystems(): Promise<void> {
    const response = await firstValueFrom(this.httpClient.get<any>(this.baseUrl + 'api/overwatch/systems')) as OverwatchSystems;
    if (response) {
      this.dataRaw = response.systems;
      this.maelstroms = response.maelstroms;
      this.maelstromsSelected = response.maelstroms;
      this.thargoidLevels = response.levels;
      this.thargoidLevelsSelected = response.levels;
      this.updateDataSource();
    }
  }

  public copySystemName(starSystem: OverwatchStarSystem): void {
    navigator.clipboard.writeText(starSystem.name);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }

  public updateDataSource(): void {
    this.dataSource = this.dataRaw.filter(d =>
      typeof this.maelstromsSelected.find(m => m.name === d.maelstrom.name) !== 'undefined' &&
      typeof this.thargoidLevelsSelected.find(t => t.level === d.thargoidLevel.level) !== 'undefined');
    this.dataSource.sort((system1: OverwatchStarSystem, system2: OverwatchStarSystem) => {
      if (system1.thargoidLevel.level > system2.thargoidLevel.level) {
        return -1;
      }
      else if (system1.thargoidLevel.level < system2.thargoidLevel.level) {
        return 1;
      }
      return system1.name.localeCompare(system2.name);
    });
    this.changeDetectorRef.detectChanges();
  }
}

interface OverwatchSystems {
  maelstroms: OverwatchMaelstrom[];
  levels: OverwatchThargoidLevel[];
  systems: OverwatchStarSystem[];
}

interface OverwatchMaelstrom {
  name: string;
  systemName: string;
}

interface OverwatchThargoidLevel {
  level: number;
  name: string;
}

interface OverwatchStarSystem {
  name: string;
  maelstrom: OverwatchMaelstrom;
  thargoidLevel: OverwatchThargoidLevel;
}
