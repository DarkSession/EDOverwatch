import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { OverwatchMaelstromProgress } from '../maelstrom-name/maelstrom-name.component';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { faClipboard } from '@fortawesome/pro-light-svg-icons';
import { faCircleQuestion } from '@fortawesome/pro-duotone-svg-icons';
import { TitanDamageResistance } from '../maelstrom/maelstrom.component';

@UntilDestroy()
@Component({
  selector: 'app-maelstroms',
  templateUrl: './maelstroms.component.html',
  styleUrls: ['./maelstroms.component.scss']
})
export class MaelstromsComponent implements OnInit {
  public readonly displayedColumns = ['Name', 'SystemName', 'State', 'DamageResistance', 'HeartsRemaining', 'HeartProgress', 'TotalProgress', 'SystemsInAlert', 'SystemsInInvasion', 'SystemsThargoidControlled', 'SystemsInRecovery'];
  @ViewChild(MatSort) sort!: MatSort;
  public readonly faClipboard = faClipboard;
  public readonly faCircleQuestion = faCircleQuestion;
  public dataSource: MatTableDataSource<OverwatchMaelstromBasic> = new MatTableDataSource<OverwatchMaelstromBasic>();

  public constructor(
    private readonly websocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar
  ) {
  }

  public ngOnInit(): void {
    this.websocketService.on<OverwatchMaelstroms>("OverwatchMaelstroms")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        if (message && message.Data) {
          this.dataSource = new MatTableDataSource<OverwatchMaelstromBasic>(message.Data.Maelstroms);
          this.dataSource.sortingDataAccessor = (maelstrom: OverwatchMaelstromBasic, columnName: string): string | number => {
            return maelstrom[columnName as keyof OverwatchMaelstromBasic] as string | number;
          }
          this.dataSource.sort = this.sort;
          this.changeDetectorRef.detectChanges();
        }
      });
    this.websocketService.sendMessage("OverwatchMaelstroms", {});
  }

  public copySystemName(maelstrom: OverwatchMaelstromBasic): void {
    navigator.clipboard.writeText(maelstrom.SystemName);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }
}

interface OverwatchMaelstroms {
  Maelstroms: OverwatchMaelstromBasic[];
}

interface OverwatchMaelstromBasic extends OverwatchMaelstromProgress {
  SystemsInAlert: number;
  SystemsInInvasion: number;
  SystemsThargoidControlled: number;
  SystemsInRecovery: number;
  DefenseRate: number;
  DamageResistance: TitanDamageResistance;
}
