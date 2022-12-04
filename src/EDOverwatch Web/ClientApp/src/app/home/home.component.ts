import { HttpClient } from '@angular/common/http';
import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import * as dayjs from 'dayjs';
import * as duration from 'dayjs/plugin/duration';
import { firstValueFrom } from 'rxjs';

dayjs.extend(duration)

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['home.component.scss'],
})
export class HomeComponent implements OnInit, OnDestroy {
  public start: dayjs.Dayjs = dayjs("2022-11-29T18:30:00.000+00:00");
  public timeSince: string = "";
  private updateInterval: any = null;
  public overview: OverwatchOverview | null = null;

  public constructor(
    private readonly httpClient: HttpClient,
    @Inject('BASE_URL') private readonly baseUrl: string) {
  }

  public ngOnInit(): void {
    this.updateInterval = setInterval(() => {
      this.updateTimeSince();
    }, 1000);
    this.updateTimeSince();
    this.loadOverview();
  }

  private async loadOverview(): Promise<void> {
    const response = await firstValueFrom(this.httpClient.get<any>(this.baseUrl + 'api/overwatch/overview'));
    if (response) {
      this.overview = response;
    }
  }

  public ngOnDestroy(): void {
    if (this.updateInterval) {
      clearInterval(this.updateInterval);
      this.updateInterval = null;
    }
  }

  private updateTimeSince(): void {
    const results: string[] = [];
    const now = dayjs();
    const duration = dayjs.duration(now.diff(this.start));

    if (duration.years() === 1) {
      results.push("1 year");
    }
    else if (duration.years() > 1) {
      results.push(`${duration.years()} years`)
    }

    if (duration.months() === 1) {
      results.push("1 month");
    }
    else if (duration.months() > 1) {
      results.push(`${duration.months()} months`)
    }

    if (duration.days() === 1) {
      results.push("1 day");
    }
    else if (duration.days() > 1) {
      results.push(`${duration.days()} days`)
    }

    if (duration.asMonths() < 1) {
      if (duration.hours() === 1) {
        results.push("1 hour");
      }
      else if (duration.hours() > 1) {
        results.push(`${duration.hours()} hours`)
      }
    }
    this.timeSince = results.join(", ");
  }
}

interface OverwatchOverview {
  humans: {
    controllingPercentage: number;
    systemsControlling: number;
    systemsRecaptured: number;
    thargoidKills: number | null;
    rescues: number | null;
    rescueSupplies: number | null;
  }
  thargoids: {
    controllingPercentage: number;
    activeMaelstroms: number;
    systemsControlling: number;
    commanderKills: number | null;
  },
  contested: {
    systemsInInvasion: number;
    systemsWithAlerts: number;
    systemsBeingRecaptured: number;
  }
}
