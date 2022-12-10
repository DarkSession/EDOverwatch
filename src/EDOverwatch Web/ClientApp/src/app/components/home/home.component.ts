import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import * as dayjs from 'dayjs';
import * as duration from 'dayjs/plugin/duration';
import { WebsocketService } from 'src/app/services/websocket.service';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';

dayjs.extend(duration)

@UntilDestroy()
@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['home.component.scss'],
})
export class HomeComponent implements OnInit, AfterViewInit, OnDestroy {
  public start: dayjs.Dayjs = dayjs("2022-11-29T18:30:00.000+00:00");
  public timeSince: string = "";
  private updateInterval: any = null;
  public overview: OverwatchOverview | null = null;
  @ViewChild("triangleLeft") triangleLeft: ElementRef | undefined;
  public sideSontentContainerMaxHeight = 2000;

  @HostListener('window:resize', ['$event'])
  onResize(event: any) {
    this.updateTriangleLeft();
  }

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly webSocketService: WebsocketService
  ) {
  }

  public ngOnInit(): void {
    this.updateInterval = setInterval(() => {
      this.updateTimeSince();
    }, 60000);
    this.webSocketService
      .on<OverwatchOverview>("OverwatchHome")
      .pipe(untilDestroyed(this))
      .subscribe((message) => {
        this.overview = message.Data;
        this.changeDetectorRef.detectChanges();
      });
    this.updateTimeSince();
    this.loadOverview();
  }

  public ngAfterViewInit(): void {
    this.updateTriangleLeft();
  }

  private updateTriangleLeft(): void {
    if (this.triangleLeft) {
      this.sideSontentContainerMaxHeight = this.triangleLeft.nativeElement.getBoundingClientRect().height;
      this.changeDetectorRef.detectChanges();
    }
  }

  private async loadOverview(): Promise<void> {
    const response = await this.webSocketService.sendMessageAndWaitForResponse<OverwatchOverview>("OverwatchHome", {});
    if (response && response.Success) {
      this.overview = response.Data;
      this.changeDetectorRef.detectChanges();
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
    this.changeDetectorRef.detectChanges();
  }
}

interface OverwatchOverview {
  Humans: {
    ControllingPercentage: number;
    SystemsControlling: number;
    SystemsRecaptured: number;
    ThargoidKills: number | null;
    Rescues: number | null;
    Missions: number | null;
  }
  Thargoids: {
    ControllingPercentage: number;
    ActiveMaelstroms: number;
    SystemsControlling: number;
    CommanderKills: number | null;
  },
  Contested: {
    SystemsInInvasion: number;
    SystemsWithAlerts: number;
    SystemsBeingRecaptured: number;
  }
}
