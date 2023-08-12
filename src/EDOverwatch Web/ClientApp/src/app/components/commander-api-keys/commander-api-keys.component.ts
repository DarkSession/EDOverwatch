import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { faClipboard } from '@fortawesome/pro-light-svg-icons';
import { UntilDestroy } from '@ngneat/until-destroy';
import { WebsocketService } from 'src/app/services/websocket.service';

@UntilDestroy()
@Component({
  selector: 'app-commander-api-keys',
  templateUrl: './commander-api-keys.component.html',
  styleUrls: ['./commander-api-keys.component.css']
})
export class CommanderApiKeysComponent implements OnInit {
  public readonly faClipboard = faClipboard;
  public apiKey: string | null = null;
  public additionalKeys: string[] = [];
  public loading = false;
  public claimAdditionalApiKey: string = "";
  public claimError: string = "";
  public readonly displayedColumns = ['APIKey'];

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly webSocketService: WebsocketService,
    private readonly matSnackBar: MatSnackBar
  ) {
  }

  public ngOnInit(): void {
    this.getApiKeys();
  }

  private async getApiKeys(): Promise<void> {
    const response = await this.webSocketService.sendMessageAndWaitForResponse<CommanderApiKeysResponse>("CommanderApiKeys", {});
    if (response?.Data) {
      this.apiKey = response.Data.ApiKey;
      this.additionalKeys = response.Data.AdditionalKeys;
    }
    this.changeDetectorRef.detectChanges();
  }

  public async generateApiKey(): Promise<void> {
    if (this.loading) {
      return;
    }
    this.loading = true;
    this.changeDetectorRef.detectChanges();
    const response = await this.webSocketService.sendMessageAndWaitForResponse<CommanderApiKeyGenerateResponse>("CommanderApiKeyGenerate", {});
    if (response?.Data) {
      this.apiKey = response.Data.ApiKey;
    }
    this.loading = false;
    this.changeDetectorRef.detectChanges();
  }

  public async claimApiKey(): Promise<void> {
    if (this.loading || !this.claimAdditionalApiKey) {
      return;
    }
    this.loading = true;
    this.claimError = "";
    const response = await this.webSocketService.sendMessageAndWaitForResponse<CommanderApiKeyClaimResponse>("CommanderApiKeyClaim", {
      Key: this.claimAdditionalApiKey,
    });
    if (response) {
      if (response.Data && response.Data.Success) {
        this.claimAdditionalApiKey = "";
        await this.getApiKeys();
      }
      else if (response.Errors && response.Errors.length) {
        this.claimError = response.Errors[0];
      }
    }
    this.loading = false;
    this.changeDetectorRef.detectChanges();
  }

  public claimAdditionalApiKeyChanged(): void {
    this.claimError = "";
    this.changeDetectorRef.detectChanges();
  }

  public copyApiKey(): void {
    if (this.apiKey) {
      navigator.clipboard.writeText(this.apiKey);
      this.matSnackBar.open("Copied to clipboard!", "Dismiss", {
        duration: 2000,
      });
    }
  }
}

interface CommanderApiKeysResponse {
  ApiKey: string | null;
  AdditionalKeys: string[];
}

interface CommanderApiKeyGenerateResponse {
  ApiKey: string;
}

interface CommanderApiKeyClaimResponse {
  Success: boolean;
}