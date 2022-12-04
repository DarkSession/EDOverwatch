import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-systems',
  templateUrl: './systems.component.html',
  styleUrls: ['./systems.component.css']
})
export class SystemsComponent implements OnInit {
  public readonly displayedColumns = ['systemName', 'thargoidLevel', 'maelstrom'];
  dataSource: OverwatchStarSystem[] = [];

  public constructor(
    private readonly httpClient: HttpClient,
    @Inject('BASE_URL') private readonly baseUrl: string,
    private readonly changeDetectorRef: ChangeDetectorRef) {
  }

  public ngOnInit(): void {
    this.loadSystems();
  }

  private async loadSystems(): Promise<void> {
    const response = await firstValueFrom(this.httpClient.get<any>(this.baseUrl + 'api/overwatch/systems')) as OverwatchSystems;
    if (response) {
      this.dataSource = response.systems;
    }
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
  starSystemThargoidLevelState: number;
  name: string;
}

interface OverwatchStarSystem {
  name: string;
  maelstrom: OverwatchMaelstrom;
  thargoidLevel: OverwatchThargoidLevel;
}
