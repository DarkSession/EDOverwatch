import { AfterViewInit, ChangeDetectorRef, Component, Input, OnChanges, ViewChild } from '@angular/core';
import { CommanderWarEffortCycleStarSystem, CommanderWarEffortCycleStarSystemWarEffort, WarEffortTypeGroup } from '../my-efforts/my-efforts.component';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { IconDefinition, faTruckRampBox } from '@fortawesome/pro-duotone-svg-icons';
import { faCrosshairs, faHandshake, faHexagonExclamation } from '@fortawesome/pro-light-svg-icons';
import { faTruck, faKitMedical } from '@fortawesome/pro-duotone-svg-icons';

@Component({
  selector: 'app-my-efforts-cycle-system',
  templateUrl: './my-efforts-cycle-system.component.html',
  styleUrls: ['./my-efforts-cycle-system.component.css']
})
export class MyEffortsCycleSystemComponent implements OnChanges, AfterViewInit {
  public readonly faHexagonExclamation = faHexagonExclamation;
  public detailDisplayedColumns = ["Date", "Type", "Amount"];
  @Input() warEffortCycleStarSystem: CommanderWarEffortCycleStarSystem | null = null;
  @ViewChild(MatSort) detailSort!: MatSort;
  public detailsDataSource: MatTableDataSource<CommanderWarEffortCycleStarSystemWarEffort> = new MatTableDataSource<CommanderWarEffortCycleStarSystemWarEffort>();
  public summary: SummaryEntry[] = [];
  public showDetails = false;

  public constructor(
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  public ngOnChanges(): void {
    if (this.warEffortCycleStarSystem) {
      this.summary = [];
      for (const warEffortEntry of this.warEffortCycleStarSystem.WarEfforts) {
        let summaryEntry =  this.summary.find(s => s.Group === warEffortEntry.Group);
        if (!summaryEntry) {
          let icon: IconDefinition = faHandshake;
          let type: string = "";
          switch (warEffortEntry.Group) {
            case WarEffortTypeGroup.Kills: {
              icon = faCrosshairs;
              type = "Kills";
              break;
            }
            case WarEffortTypeGroup.Rescue: {
              icon = faKitMedical;
              type = "Rescues";
              break;
            }
            case WarEffortTypeGroup.Supply: {
              icon = faTruck;
              type = "Supplies";
              break;
            }
            case WarEffortTypeGroup.Mission: {
              type = "Missions";
              break;
            }
            case WarEffortTypeGroup.RecoveryAndProbing: {
              icon = faTruckRampBox;
              type = "Recovery and probing";
              break;
            }
            default: {
              type = "Other";
              icon = faHandshake;
              break;
            }
          }
          summaryEntry = {
            Group: warEffortEntry.Group,
            Total: warEffortEntry.Amount,
            Icon: icon,
            Type: type,
            Entries: [
              {
                Type: warEffortEntry.Type,
                Total: warEffortEntry.Amount,
              },
            ],
          };
          this.summary.push(summaryEntry);
        }
        else {
          summaryEntry.Total += warEffortEntry.Amount;
          let detailEntry = summaryEntry.Entries.find(e => e.Type === warEffortEntry.Type);
          if (!detailEntry) {
            detailEntry = {
              Type: warEffortEntry.Type,
              Total: warEffortEntry.Amount,
            };
            summaryEntry.Entries.push(detailEntry);
          }
          else {
            detailEntry.Total += warEffortEntry.Amount;
          }
        }
      }
    }
  }

  public ngAfterViewInit(): void {
    if (this.warEffortCycleStarSystem) {
      this.detailsDataSource = new MatTableDataSource<CommanderWarEffortCycleStarSystemWarEffort>(this.warEffortCycleStarSystem.WarEfforts);
      this.detailsDataSource.sortingDataAccessor = (warEffort: CommanderWarEffortCycleStarSystemWarEffort, columnName: string): string => {
        return warEffort[columnName as keyof CommanderWarEffortCycleStarSystemWarEffort] as string;
      }
      this.detailsDataSource.sort = this.detailSort;
    }
  }
  
  public toggleShowDetails(): void {
    this.showDetails = !this.showDetails;
    this.changeDetectorRef.detectChanges();
    this.ngAfterViewInit();
    this.changeDetectorRef.detectChanges();
  }
}

interface SummaryEntry {
  Group: WarEffortTypeGroup;
  Total: number;
  Type: string;
  Icon: IconDefinition;
  Entries: {
    Type: string;
    Total: number;
  }[];
}