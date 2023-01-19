import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MapHistoricalComponent } from './components/map-historical/map-historical.component';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { CurrentComponent } from './components/current/current.component';


@NgModule({
  declarations: [
    MapHistoricalComponent,
    CurrentComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
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
        path: 'current',
        component: CurrentComponent,
      },
    ])
  ]
})
export class EdmapModule { }
