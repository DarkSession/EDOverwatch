import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { faFileCsv } from '@fortawesome/pro-duotone-svg-icons';
import { UntilDestroy } from '@ngneat/until-destroy';
import { mkConfig, generateCsv, download } from "export-to-csv";
import { AppService } from 'src/app/services/app.service';
import { WebsocketService } from 'src/app/services/websocket.service';
import { OverwatchStarSystem } from '../system-list/system-list.component';
import { faHexagonExclamation } from '@fortawesome/pro-light-svg-icons';

@UntilDestroy()
@Component({
  selector: 'app-commander-fc-cargo',
  templateUrl: './commander-fc-cargo.component.html',
  styleUrls: ['./commander-fc-cargo.component.css']
})
export class CommanderFcCargoComponent implements OnInit {
  public readonly faFileCsv = faFileCsv;
  public readonly faHexagonExclamation = faHexagonExclamation;
  public pageSize: number = 50;
  public displayedColumns = ['Commodity', 'SystemName', 'SystemState', 'Quantity', 'StackNumber', 'Changed'];
  public loading = false;
  public fleetCarrierCargo: CommanderFleetCarrierCargo | null = null;
  public cargoEntries: MatTableDataSource<CommanderFleetCarrierCargoEntry> = new MatTableDataSource<CommanderFleetCarrierCargoEntry>();
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator | null = null;
  public showDetailedStacks = false;

  public constructor(
    private readonly appService: AppService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly webSocketService: WebsocketService
  ) {
  }

  public ngOnInit(): void {
    this.getFcCargo();
    this.updateSettings();
  }

  private async updateSettings(): Promise<void> {
    this.showDetailedStacks = (await this.appService.getSetting("FleetCarrierCargoShowStacks") === "1");
    this.updateCargoEntries();
    this.changeDetectorRef.detectChanges();
  }

  private async getFcCargo(): Promise<void> {
    const response = await this.webSocketService.sendMessageAndWaitForResponse<CommanderFleetCarrierCargo>("CommanderFleetCarrierCargo", {});
    if (response?.Data) {
      this.fleetCarrierCargo = response.Data;
      this.updateCargoEntries();
    }
  }

  public detailedStacksToggle(): void {
    this.appService.saveSetting("FleetCarrierCargoShowStacks", this.showDetailedStacks ? "1" : "0");
    this.updateCargoEntries();
  }

  private updateCargoEntries(): void {
    if (!this.fleetCarrierCargo) {
      return;
    }
    let cargo: CommanderFleetCarrierCargoEntry[] = this.fleetCarrierCargo.Cargo;
    if (!this.showDetailedStacks) {
      this.displayedColumns = ['Commodity', 'SystemName', 'SystemState', 'Quantity'];
      cargo = [];
      for (const cargoEntry of this.fleetCarrierCargo.Cargo) {
        const existingEntry = cargo.find(c => c.Commodity == cargoEntry.Commodity && c.StarSystem.SystemAddress == cargoEntry.StarSystem.SystemAddress);
        if (existingEntry) {
          existingEntry.Quantity += cargoEntry.Quantity;
        }
        else {
          cargo.push({
            Commodity: cargoEntry.Commodity,
            StarSystem: cargoEntry.StarSystem,
            Quantity: cargoEntry.Quantity,
            StackNumber: 0,
            Changed: "",
          });
        }
      }
    }
    else {
      this.displayedColumns = ['Commodity', 'SystemName', 'SystemState', 'Quantity', 'StackNumber', 'Changed'];
    }
    this.cargoEntries = new MatTableDataSource<CommanderFleetCarrierCargoEntry>(cargo);
    this.cargoEntries.paginator = this.paginator;
    this.cargoEntries.sortingDataAccessor = (warEffort: CommanderFleetCarrierCargoEntry, columnName: string): string | number => {
      switch (columnName) {
        case "SystemState": {
          return warEffort.StarSystem.ThargoidLevel.Level;
        }
        case "SystemName": {
          return warEffort.StarSystem.Name;
        }
      }
      return warEffort[columnName as keyof CommanderFleetCarrierCargoEntry] as string;
    }
    this.cargoEntries.sort = this.sort;
    this.changeDetectorRef.detectChanges();
  }

  public exportToCsv(): void {
    const data = [];
    for (const cargo of this.cargoEntries.data) {
      data.push({
        Commodity: cargo.Commodity,
        SystemName: cargo.StarSystem.Name,
        SystemAddress: cargo.StarSystem.SystemAddress,
        Quantity: cargo.Quantity,
      });
    }

    const csvConfig = mkConfig({
      fieldSeparator: ',',
      quoteStrings: true,
      decimalSeparator: '.',
      showTitle: false,
      filename: "Overwatch Fleet Carrier Cargo Export",
      useTextFile: false,
      useBom: true,
      useKeysAsHeaders: true,
    });

    const csv = generateCsv(csvConfig)(data);
    download(csvConfig)(csv);
  }
}

interface CommanderFleetCarrierCargo {
  LastUpdated: string;
  HasFleetCarrier: boolean;
  Cargo: CommanderFleetCarrierCargoEntry[];
}

interface CommanderFleetCarrierCargoEntry {
  Commodity: string;
  StarSystem: OverwatchStarSystem;
  Quantity: number;
  StackNumber: number;
  Changed: string;
}