import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { faClipboard } from '@fortawesome/pro-light-svg-icons';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { FactionOperation } from '../system/system.component';
import { OverwatchThargoidLevel } from '../thargoid-level/thargoid-level.component';

@Component({
  selector: 'app-operation-search',
  templateUrl: './operation-search.component.html',
  styleUrls: ['./operation-search.component.scss']
})
export class OperationSearchComponent implements OnInit {
  public readonly faClipboard = faClipboard;
  public maelstromSelected: string = "";
  public systemName: string = "";
  public maelstroms: string[] = [];
  public operations: MatTableDataSource<FactionOperationStarSystemLevel> = new MatTableDataSource<FactionOperationStarSystemLevel>();
  public searchNotFound = false;
  public operationTypeSelected: {
    key: number;
    value: string;
  } | null = null;
  public operationTypes: {
    key: number;
    value: string;
  }[] = [
      {
        key: DcohFactionOperationType.AXCombat,
        value: "AX Combat",
      },
      {
        key: DcohFactionOperationType.Logistics,
        value: "Logistics",
      },
      {
        key: DcohFactionOperationType.Rescue,
        value: "Rescue",
      },
    ];
  public readonly operationsDisplayedColumns = ['Faction', 'Type', 'Started', 'SystemName', 'ThargoidLevel', 'Maelstrom'];
  @ViewChild(MatSort) sort!: MatSort;

  public constructor(
    private readonly webSocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly matSnackBar: MatSnackBar
  ) {
  }

  public ngOnInit(): void {
    this.queryServer();
  }

  public settingChanged(): void {
    this.queryServer();
  }

  public copySystemName(factionOperation: FactionOperation): void {
    navigator.clipboard.writeText(factionOperation.SystemName);
    this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
      duration: 2000,
    });
  }

  private async queryServer(): Promise<void> {
    const response = await this.webSocketService.sendMessageAndWaitForResponse<OverwatchOperationSearchResponse>("OverwatchOperationSearch", {
      Type: this.operationTypeSelected?.key,
      Maelstrom: this.maelstromSelected,
      SystemName: this.systemName,
    });
    if (response && response.Data) {
      this.maelstroms = response.Data.Maelstroms.map(m => m.Name);
      this.maelstroms.unshift("");
      if (response.Data.Operations) {
        this.operations = new MatTableDataSource<FactionOperationStarSystemLevel>(response.Data.Operations);
        this.operations.sortingDataAccessor = (factionOperation: FactionOperationStarSystemLevel, columnName: string): string | number => {
          switch (columnName) {
            case "ThargoidLevel": {
              return factionOperation.ThargoidLevel.Level;
            }
            case "Maelstrom": {
              return factionOperation.Maelstrom.Name;
            }
            /*
case "Starports": {
  return (factionOperation.StationsUnderAttack + factionOperation.StationsDamaged + factionOperation.StationsUnderRepair);
}
*/
          }
          return factionOperation[columnName as keyof FactionOperationStarSystemLevel] as string;
        }
        this.operations.sort = this.sort;
        this.searchNotFound = false;
      }
      else if (this.systemName) {
        this.searchNotFound = true;
      }
      this.changeDetectorRef.detectChanges();
    }
  }
}

enum DcohFactionOperationType {
  AXCombat = 1,
  Rescue,
  Logistics,
}

interface OverwatchOperationSearchResponse {
  Maelstroms: OverwatchMaelstrom[];
  Operations: FactionOperationStarSystemLevel[] | null;
}

interface FactionOperationStarSystemLevel extends FactionOperation {
  ThargoidLevel: OverwatchThargoidLevel;
  Maelstrom: OverwatchMaelstrom;
}