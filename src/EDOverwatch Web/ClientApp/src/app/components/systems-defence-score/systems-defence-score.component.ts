import { Component, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ExportToCsv, Options } from 'export-to-csv';
import { faFileCsv } from '@fortawesome/free-solid-svg-icons';

@UntilDestroy()
@Component({
  selector: 'app-systems-defence-score',
  templateUrl: './systems-defence-score.component.html',
  styleUrls: ['./systems-defence-score.component.css']
})
export class SystemsDefenceScoreComponent implements OnInit {
  public readonly displayedColumns = ['Name', 'DefenseScore'];
  @ViewChild(MatSort) sort!: MatSort;
  public readonly faFileCsv = faFileCsv;
  public dataSource: MatTableDataSource<SystemDefenseScore> = new MatTableDataSource<SystemDefenseScore>();

  public constructor(
    private readonly websocketService: WebsocketService,
  ) {
  }

  public ngOnInit(): void {
    this.websocketService
      .on<OverwatchSystemDefenseScores>("OverwatchSystemDefenseScore")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message && message.Data) {
          this.dataSource = new MatTableDataSource<SystemDefenseScore>(message.Data.Systems);
          this.dataSource.sortingDataAccessor = (maelstrom: SystemDefenseScore, columnName: string): string | number => {
            return maelstrom[columnName as keyof SystemDefenseScore] as string | number;
          }
          this.dataSource.sort = this.sort;
        }
      });
    this.websocketService.sendMessage("OverwatchSystemDefenseScore", {});
  }

  public exportToCsv(): void {
    const data = [];
    for (const system of this.dataSource.data) {
      data.push({
        Name: system.Name,
        // SystemAddress: system.SystemAddress,
        DefenseScore: system.DefenseScore,
      });
    }

    const options: Options = {
      fieldSeparator: ',',
      quoteStrings: '"',
      decimalSeparator: '.',
      showLabels: true,
      showTitle: false,
      filename: "Overwatch System Defense Score",
      useTextFile: false,
      useBom: true,
      useKeysAsHeaders: true,
    };

    const csvExporter = new ExportToCsv(options);
    csvExporter.generateCsv(data);
  }
}

interface OverwatchSystemDefenseScores {
  Systems: SystemDefenseScore[];
}

interface SystemDefenseScore {
  Name: string;
  DefenseScore: number;
}