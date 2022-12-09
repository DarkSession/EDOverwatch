import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-my-efforts',
  templateUrl: './my-efforts.component.html',
  styleUrls: ['./my-efforts.component.css']
})
export class MyEffortsComponent implements OnInit {
  public readonly displayedColumns = ['date', 'systemName', 'type', 'amount'];
  public dataSource: CommanderWarEffort[] = [];

  public constructor(
    private readonly httpClient: HttpClient,
    @Inject('API_URL') private readonly apiUrl: string,
    private readonly changeDetectorRef: ChangeDetectorRef
    ) {
  }

  public ngOnInit(): void {
    this.loadMyEfforts();
  }

  public async loadMyEfforts(): Promise<void> {
    const response = await firstValueFrom(this.httpClient.post<CommanderWarEffort[]>(this.apiUrl + 'commander/GetWarEfforts', {}, {
      withCredentials: true,
    }));
    this.dataSource = response;
    this.changeDetectorRef.detectChanges();
  }
}

interface CommanderWarEffort {
  date: string;
  type: string;
  systemName: string;
  amount: number;
}