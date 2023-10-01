/* "Barrel" of Http Interceptors */
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { HttpRequestInterceptor } from './services/HttpRequestInterceptor';

import { BrowserModule } from '@angular/platform-browser';
import { NgModule, isDevMode } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouteReuseStrategy, RouterModule } from '@angular/router';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatTableModule } from '@angular/material/table';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSortModule } from '@angular/material/sort';
import { TimeagoModule } from 'ngx-timeago';
import { MatTabsModule } from '@angular/material/tabs';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatMenuModule } from '@angular/material/menu';

import { AuthenticationGuard } from './guards/authentication.guard';

import { AppComponent } from './app.component';
import { AboutComponent } from './components/about/about.component';
import { GetInvolvedComponent } from './components/get-involved/get-involved.component';
import { ConsumerApiComponent } from './components/consumer-api/consumer-api.component';
import { AuthComponent } from './components/auth/auth.component';
import { MyEffortsComponent } from './components/my-efforts/my-efforts.component';
import { SystemComponent } from './components/system/system.component';
import { MaelstromComponent } from './components/maelstrom/maelstrom.component';
import { MaelstromNameComponent } from './components/maelstrom-name/maelstrom-name.component';
import { SystemListComponent } from './components/system-list/system-list.component';
import { NgChartsModule } from 'ng2-charts';
import { FaqComponent } from './components/faq/faq.component';
import { ThargoidLevelComponent } from './components/thargoid-level/thargoid-level.component';
import { SystemStarportStatusComponent } from './components/system-starport-status/system-starport-status.component';
import { ServiceWorkerModule } from '@angular/service-worker';
import { StationNameComponent } from './components/station-name/station-name.component';
import { SystemStationsComponent } from './components/system-stations/system-stations.component';
import { SystemOperationsComponent } from './components/system-operations/system-operations.component';
import { SystemContributionsComponent } from './components/system-contributions/system-contributions.component';
import { MaelstromsComponent } from './components/maelstroms/maelstroms.component';
import { DateAgoPipe } from './pipes/date-ago.pipe';
import { OperationSearchComponent } from './components/operation-search/operation-search.component';
import { NumberSuffixPipe } from './pipes/number-suffix.pipe';
import { SystemHistoryComponent } from './components/system-history/system-history.component';
import { SystemStateAnalysisComponent } from './components/system-state-analysis/system-state-analysis.component';
import { ContributeDataComponent } from './components/contribute-data/contribute-data.component';
import { CommanderApiKeysComponent } from './components/commander-api-keys/commander-api-keys.component';
import { SystemContributionSummaryComponent } from './components/system-contribution-summary/system-contribution-summary.component';
import { Chart } from 'chart.js';
import { StatsComponent } from './components/stats/stats.component';
import ChartDataLabels from 'chartjs-plugin-datalabels';
import Annotation from 'chartjs-plugin-annotation';
import * as duration from 'dayjs/plugin/duration';
import * as utc from 'dayjs/plugin/utc';
import * as dayjs from 'dayjs';
import { CommanderFcCargoComponent } from './components/commander-fc-cargo/commander-fc-cargo.component';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AlertPredictionAttackersComponent } from './components/alert-prediction-attackers/alert-prediction-attackers.component';
import { AlertPredictionComponent } from './components/alert-prediction/alert-prediction.component';
import { AppRouteReuseStrategy } from './app-route-reuse.strategy';
import { HomeV2Component } from './components/home-v2/home-v2.component';
import { HomeV2CycleComponent } from './components/home-v2-cycle/home-v2-cycle.component';
import { HomeV2CycleChangesComponent } from './components/home-v2-cycle-changes/home-v2-cycle-changes.component';
import { NgxSkeletonLoaderModule } from 'ngx-skeleton-loader';
import { AlertPredictionOverviewComponent } from './components/alert-prediction-overview/alert-prediction-overview.component';
import { SystemsHistoricalCycleComponent } from './components/systems-historical-cycle/systems-historical-cycle.component';
import { SystemFeaturesComponent } from './components/system-features/system-features.component';
import { MyEffortsCycleComponent } from './components/my-efforts-cycle/my-efforts-cycle.component';
import { MyEffortsCycleSystemComponent } from './components/my-efforts-cycle-system/my-efforts-cycle-system.component';
import { MyEffortsCycleSystemEffortTypeComponent } from './components/my-efforts-cycle-system-effort-type/my-efforts-cycle-system-effort-type.component';
import { AlertPredictionTopAttackersComponent } from './components/alert-prediction-top-attackers/alert-prediction-top-attackers.component';
import { SystemAttackDefenseComponent } from './components/system-attack-defense/system-attack-defense.component';
import { ExperimentalSettingsComponent } from './components/experimental-settings/experimental-settings.component';

Chart.defaults.color = "#cccccc";
Chart.defaults.borderColor = "rgba(255,255,255,0.15)";
Chart.register(Annotation);
Chart.register(ChartDataLabels);

dayjs.extend(duration)
dayjs.extend(utc);

/** Http interceptor providers in outside-in order */
export const httpInterceptorProviders = [
  { provide: HTTP_INTERCEPTORS, useClass: HttpRequestInterceptor, multi: true },
];

@NgModule({
  declarations: [
    AppComponent,
    AboutComponent,
    GetInvolvedComponent,
    ConsumerApiComponent,
    AuthComponent,
    MyEffortsComponent,
    SystemComponent,
    MaelstromComponent,
    MaelstromNameComponent,
    SystemListComponent,
    FaqComponent,
    ThargoidLevelComponent,
    SystemStarportStatusComponent,
    StationNameComponent,
    SystemStationsComponent,
    SystemOperationsComponent,
    SystemContributionsComponent,
    MaelstromsComponent,
    DateAgoPipe,
    OperationSearchComponent,
    NumberSuffixPipe,
    SystemHistoryComponent,
    SystemStateAnalysisComponent,
    ContributeDataComponent,
    CommanderApiKeysComponent,
    SystemContributionSummaryComponent,
    StatsComponent,
    CommanderFcCargoComponent,
    AlertPredictionAttackersComponent,
    AlertPredictionComponent,
    HomeV2Component,
    HomeV2CycleComponent,
    HomeV2CycleChangesComponent,
    AlertPredictionOverviewComponent,
    SystemsHistoricalCycleComponent,
    SystemFeaturesComponent,
    MyEffortsCycleComponent,
    MyEffortsCycleSystemComponent,
    MyEffortsCycleSystemEffortTypeComponent,
    AlertPredictionTopAttackersComponent,
    SystemAttackDefenseComponent,
    ExperimentalSettingsComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    TimeagoModule.forRoot(),
    HttpClientModule,
    FormsModule,
    BrowserAnimationsModule,
    MatTableModule,
    MatSortModule,
    MatSelectModule,
    MatTooltipModule,
    MatSnackBarModule,
    FontAwesomeModule,
    MatSidenavModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatProgressBarModule,
    MatTabsModule,
    MatInputModule,
    MatFormFieldModule,
    NgChartsModule,
    MatPaginatorModule,
    MatCheckboxModule,
    MatMenuModule,
    MatSlideToggleModule,
    NgxSkeletonLoaderModule.forRoot({
      theme: {
        // Enabliong theme combination
        extendsFromRoot: true,
        // ... list of CSS theme attributes
        height: '16px',
      },
    }),
    RouterModule.forRoot([
      {
        path: 'about',
        component: AboutComponent
      },
      {
        path: 'auth',
        component: AuthComponent
      },
      {
        path: 'alert-predictions',
        component: AlertPredictionOverviewComponent,
      },
      {
        path: 'commander/api-keys',
        component: CommanderApiKeysComponent,
        canActivate: [AuthenticationGuard],
      },
      {
        path: 'commander/fleet-carrier-cargo',
        component: CommanderFcCargoComponent,
        canActivate: [AuthenticationGuard],
      },
      {
        path: 'consumer-api',
        component: ConsumerApiComponent,
      },
      {
        path: 'contribute',
        component: ContributeDataComponent,
      },
      {
        path: 'faq',
        component: FaqComponent,
      },
      {
        path: 'get-involved',
        component: GetInvolvedComponent
      },
      {
        path: 'join-the-fight',
        component: GetInvolvedComponent,
      },
      {
        path: 'maelstrom/:name',
        component: MaelstromComponent,
      },
      {
        path: 'titan/:name',
        component: MaelstromComponent,
      },
      {
        path: 'maelstroms',
        component: MaelstromsComponent,
        data: {
          reuseRoute: true,
        },
      },
      {
        path: 'titans',
        component: MaelstromsComponent,
        data: {
          reuseRoute: true,
        },
      },
      {
        path: 'edmap',
        loadChildren: () => import('./edmap/edmap.module').then(m => m.EdmapModule)
      },
      {
        path: 'map',
        loadChildren: () => import('./edmap/edmap.module').then(m => m.EdmapModule)
      },
      {
        path: 'my-efforts',
        component: MyEffortsComponent,
        canActivate: [AuthenticationGuard],
      },
      {
        path: 'operation-search',
        component: OperationSearchComponent,
        data: {
          reuseRoute: true,
        },
      },
      {
        path: 'operations',
        component: OperationSearchComponent,
      },
      {
        path: 'stats',
        component: StatsComponent,
        data: {
          reuseRoute: true,
        },
      },
      {
        path: 'system/:id/analyze/:cycle',
        component: SystemStateAnalysisComponent,
      },
      {
        path: 'system/:id',
        component: SystemComponent,
      },
      {
        path: 'systems',
        component: HomeV2Component,
        data: {
          reuseRoute: true,
        },
      },
      {
        path: 'systems-cycle/:date',
        component: SystemsHistoricalCycleComponent,
      },
      {
        path: 'systems-cycle',
        component: SystemsHistoricalCycleComponent,
      },
      {
        path: "experimental-settings",
        component: ExperimentalSettingsComponent,
      },
      {
        path: '**',
        component: HomeV2Component,
      },
    ]),
    ServiceWorkerModule.register('ngsw-worker.js', {
      enabled: !isDevMode(),
      // Register the ServiceWorker as soon as the application is stable
      // or after 30 seconds (whichever comes first).
      registrationStrategy: 'registerWhenStable:30000'
    }),
  ],
  providers: [
    httpInterceptorProviders,
    {
      provide: RouteReuseStrategy,
      useClass: AppRouteReuseStrategy,
    },
  ],
  bootstrap: [AppComponent],

})
export class AppModule { }
