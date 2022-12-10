import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';

@UntilDestroy()
@Component({
  selector: 'app-my-efforts',
  templateUrl: './my-efforts.component.html',
  styleUrls: ['./my-efforts.component.css']
})
export class MyEffortsComponent implements OnInit {
  public readonly displayedColumns = ['Date', 'SystemName', 'Type', 'Amount'];
  public dataSource: CommanderWarEffort[] = [];

  public constructor(
    private readonly webSocketService: WebsocketService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.loadMyEfforts();
    this.webSocketService
      .on<CommanderWarEffort[]>("CommanderWarEfforts")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.dataSource = message.Data;
        this.changeDetectorRef.detectChanges();
      });
  }

  public async loadMyEfforts(): Promise<void> {
    const response = await this.webSocketService.sendMessageAndWaitForResponse<CommanderWarEffort[]>("CommanderWarEfforts", {});
    if (response && response.Success) {
      this.dataSource = response.Data;
    }
    this.changeDetectorRef.detectChanges();
  }
}

interface CommanderWarEffort {
  Date: string;
  Type: string;
  SystemName: string;
  Amount: number;
}