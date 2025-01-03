import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MapHistoricalComponent } from './components/map-historical/map-historical.component';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { CurrentComponent } from './components/current/current.component';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { MatTooltipModule } from '@angular/material/tooltip';

@NgModule({
  declarations: [
    MapHistoricalComponent,
    CurrentComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    FontAwesomeModule,
    MatTooltipModule,
    MatFormFieldModule,
    MatSelectModule,
    RouterModule.forChild([
      {
        path: 'historical/:date',
        component: MapHistoricalComponent,
      },
      {
        path: 'historical',
        component: MapHistoricalComponent,
      },
      {
        path: '**',
        component: CurrentComponent,
      },
    ])
  ]
})
export class EdmapModule { }
