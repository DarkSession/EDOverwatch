import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { faFileCsv } from '@fortawesome/free-solid-svg-icons';
import { ExportToCsv, Options } from 'export-to-csv';
import { OverwatchThargoidCycle } from '../home/home.component';
import { ChartConfiguration, ChartDataset, Color } from 'chart.js';
import { Context } from 'chartjs-plugin-datalabels';

@UntilDestroy()
@Component({
  selector: 'app-my-efforts',
  templateUrl: './my-efforts.component.html',
  styleUrls: ['./my-efforts.component.css']
})
export class MyEffortsComponent implements OnInit {
  public readonly displayedColumns = ['Date', 'SystemName', 'Type', 'Amount'];
  public readonly faFileCsv = faFileCsv;
  public dataSource: MatTableDataSource<CommanderWarEffort> = new MatTableDataSource<CommanderWarEffort>();
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator | null = null;
  public pageSize: number = 50;
  public chartConfig: ChartConfiguration = {
    type: 'bar',
    data: {
      datasets: [],
      labels: [],
    },
    options: {
      responsive: true,
    },
  };
  private chartLoaded: boolean = false;

  public constructor(
    private readonly webSocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.webSocketService
      .on<CommanderWarEfforts>("CommanderWarEffortsV2")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.updateEfforts(message.Data);
      });
    this.webSocketService.sendMessage("CommanderWarEffortsV2", {});
  }

  private updateEfforts(data: CommanderWarEfforts): void {
    this.dataSource = new MatTableDataSource<CommanderWarEffort>(data.WarEfforts);
    this.dataSource.paginator = this.paginator;
    this.dataSource.sortingDataAccessor = (warEffort: CommanderWarEffort, columnName: string): string => {
      return warEffort[columnName as keyof CommanderWarEffort] as string;
    }
    this.dataSource.sort = this.sort;

    const labels = [];
    for (const thargoidCycle of data.RecentThargoidCycles) {
      labels.push(thargoidCycle.StartDate + " to " + thargoidCycle.EndDate);
    }

    const datasets: ChartDataset<'bar', number[]>[] = [];
    for (const warEffortTypeGroup of data.WarEffortTypeGroups) {
      const dataset = {
        label: warEffortTypeGroup,
        data: labels.map(t => 0),
      };
      datasets.push(dataset);
      for (const warEffort of data.WarEfforts.filter(w => w.TypeGroup === warEffortTypeGroup)) {
        const index = data.RecentThargoidCycles.findIndex(t => t.Cycle === warEffort.Cycle);
        if (index !== -1) {
          dataset.data[index] += warEffort.Amount;
        }
      }
    }

    this.chartConfig = {
      type: 'bar',
      data: {
        datasets: [...datasets],
        labels: labels,
      },
      options: {
        responsive: true,
        plugins: {
          datalabels: {
            align: 'center',
            anchor: 'center',
            color: 'black',
            backgroundColor: (context: Context) => {
              return context.dataset.backgroundColor as Color;
            },
            display: (context) => {
              return !!context.dataset.data[context.dataIndex];
            },
            borderRadius: 4,
          }
        }
      },
    };
    if (this.chartLoaded) {
      this.chartConfig.options!.animation = false;
    }
    this.chartLoaded = true;

    this.changeDetectorRef.detectChanges();
  }

  public exportToCsv(): void {
    const data = [];
    for (const warEffort of this.dataSource.data) {
      data.push({
        Date: warEffort.Date,
        Type: warEffort.Type,
        System: warEffort.SystemName,
        Amount: warEffort.Amount,
      });
    }

    const options: Options = {
      fieldSeparator: ',',
      quoteStrings: '"',
      decimalSeparator: '.',
      showLabels: true,
      showTitle: false,
      filename: "Overwatch War Contributions Export",
      useTextFile: false,
      useBom: true,
      useKeysAsHeaders: true,
    };

    const csvExporter = new ExportToCsv(options);
    csvExporter.generateCsv(data);
  }
}

interface CommanderWarEfforts {
  WarEfforts: CommanderWarEffort[];
  WarEffortTypeGroups: string[];
  RecentThargoidCycles: OverwatchThargoidCycle[];
}

interface CommanderWarEffort {
  Date: string;
  Type: string;
  TypeGroup: string;
  SystemName: string;
  Amount: number;
  Cycle: string;
}
