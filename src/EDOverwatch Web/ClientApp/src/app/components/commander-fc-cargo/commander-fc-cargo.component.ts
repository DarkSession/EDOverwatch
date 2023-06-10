import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';

@UntilDestroy()
@Component({
  selector: 'app-commander-fc-cargo',
  templateUrl: './commander-fc-cargo.component.html',
  styleUrls: ['./commander-fc-cargo.component.css']
})
export class CommanderFcCargoComponent implements OnInit {
  public pageSize: number = 50;
  public readonly displayedColumns = ['Commodity', 'SystemName', 'Quantity'];
  public loading = false;
  public fleetCarrierCargo: CommanderFleetCarrierCargo | null = null;
  public cargoEntries: MatTableDataSource<CommanderFleetCarrierCargoEntry> | null = null;

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly webSocketService: WebsocketService
  ) {
  }

  public ngOnInit(): void {
    this.getFcCargo();
  }

  private async getFcCargo(): Promise<void> {
    const response = await this.webSocketService.sendMessageAndWaitForResponse<CommanderFleetCarrierCargo>("CommanderApiKeys", {});
    if (response?.Data) {
      this.fleetCarrierCargo = response.Data;
      this.cargoEntries = new MatTableDataSource<CommanderFleetCarrierCargoEntry>(response.Data.Cargo);
    }
    this.changeDetectorRef.detectChanges();
  }
}

interface CommanderFleetCarrierCargo {
  LastUpdated: string;
  HasFleetCarrier: boolean;
  Cargo: CommanderFleetCarrierCargoEntry[];
}

interface CommanderFleetCarrierCargoEntry {
  Commodity: string;
  StarSystem: 
  {
    SystemAddress: number;
    Name: string;
  };
  Quantity: number;
}