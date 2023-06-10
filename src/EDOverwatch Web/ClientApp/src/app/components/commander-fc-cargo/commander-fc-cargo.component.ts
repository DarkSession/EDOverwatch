import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';

@UntilDestroy()
@Component({
  selector: 'app-commander-fc-cargo',
  templateUrl: './commander-fc-cargo.component.html',
  styleUrls: ['./commander-fc-cargo.component.css']
})
export class CommanderFcCargoComponent implements OnInit {
  public loading = false;
  public fleetCarrierCargo: CommanderFleetCarrierCargo | null = null;

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