import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { AppService } from 'src/app/services/app.service';

@Component({
  selector: 'app-experimental-settings',
  templateUrl: './experimental-settings.component.html',
  styleUrls: ['./experimental-settings.component.css']
})
export class ExperimentalSettingsComponent implements OnInit {
  public effortEstimates = false;
  public progressDetails = false;

  public constructor(
    private readonly appService: AppService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnInit(): void {
    this.updateSettings();
  }

  private async updateSettings(): Promise<void> {
    this.effortEstimates = (await this.appService.getSetting("ExperimentalEffortEstimates")) === "1";
    this.progressDetails = (await this.appService.getSetting("ExperimentalProgressDetails")) === "1";
    this.changeDetectorRef.detectChanges();
  }

  public async saveSettings(): Promise<void> {
    await this.appService.saveSetting("ExperimentalEffortEstimates", this.effortEstimates ? "1" : "0");
    await this.appService.saveSetting("ExperimentalProgressDetails", this.progressDetails ? "1" : "0");
  }
}
