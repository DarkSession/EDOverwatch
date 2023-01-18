import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MapHistoricalComponent } from './components/map-historical/map-historical.component';



@NgModule({
  declarations: [
    MapHistoricalComponent
  ],
  imports: [
    CommonModule,

    RouterModule.forChild([
      {
        path: 'historical',
        component: MapHistoricalComponent,
      },
    ])
  ]
})
export class EdmapModule { }
