import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { faClipboard } from '@fortawesome/free-regular-svg-icons';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchMaelstrom } from '../maelstrom-name/maelstrom-name.component';
import { FactionOperation } from '../system/system.component';

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
  public operations: MatTableDataSource<FactionOperation> = new MatTableDataSource<FactionOperation>();
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
  public readonly operationsDisplayedColumns = ['SystemName', 'Faction', 'Type', 'Started'];
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
        this.operations = new MatTableDataSource<FactionOperation>(response.Data.Operations);
        this.operations.sortingDataAccessor = (factionOperation: FactionOperation, columnName: string): string => {
          return factionOperation[columnName as keyof FactionOperation] as string;
        }
        this.operations.sort = this.sort;
        this.searchNotFound = false;
      }
      else if (this.operationTypeSelected && (this.maelstromSelected || this.systemName)) {
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
  Operations: FactionOperation[] | null;
}