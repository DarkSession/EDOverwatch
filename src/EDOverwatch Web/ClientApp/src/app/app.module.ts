/* "Barrel" of Http Interceptors */
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { HttpRequestInterceptor } from './services/HttpRequestInterceptor';

import { BrowserModule } from '@angular/platform-browser';
import { NgModule, isDevMode } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
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

import { AuthenticationGuard } from './guards/authentication.guard';

import { AppComponent } from './app.component';
import { HomeComponent } from './components/home/home.component';
import { SystemsComponent } from './components/systems/systems.component';
import { AboutComponent } from './components/about/about.component';
import { GetInvolvedComponent } from './components/get-involved/get-involved.component';
import { ConsumerApiComponent } from './components/consumer-api/consumer-api.component';
import { LoginComponent } from './components/login/login.component';
import { AuthComponent } from './components/auth/auth.component';
import { MyEffortsComponent } from './components/my-efforts/my-efforts.component';
import { SystemComponent } from './components/system/system.component';
import { NotAuthenticatedGuard } from './guards/not-authenticated.guard';
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


/** Http interceptor providers in outside-in order */
export const httpInterceptorProviders = [
  { provide: HTTP_INTERCEPTORS, useClass: HttpRequestInterceptor, multi: true },
];

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    SystemsComponent,
    AboutComponent,
    GetInvolvedComponent,
    ConsumerApiComponent,
    LoginComponent,
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
    RouterModule.forRoot([
      {
        path: 'about',
        component: AboutComponent
      },
      {
        path: 'auth',
        component: AuthComponent,
        canActivate: [NotAuthenticatedGuard],
      },
      {
        path: 'consumer-api',
        component: ConsumerApiComponent,
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
        path: 'login',
        component: LoginComponent,
        canActivate: [NotAuthenticatedGuard],
      },
      {
        path: 'maelstrom/:name',
        component: MaelstromComponent,
      },
      {
        path: 'maelstroms',
        component: MaelstromsComponent,
      },
      {
        path: 'my-efforts',
        component: MyEffortsComponent,
        canActivate: [AuthenticationGuard],
      },
      {
        path: 'operation-search',
        component: OperationSearchComponent,
      },
      {
        path: 'system/:id',
        component: SystemComponent
      },
      {
        path: 'systems',
        component: SystemsComponent
      },
      {
        path: '**',
        component: HomeComponent
      },
    ]),
    ServiceWorkerModule.register('ngsw-worker.js', {
      enabled: !isDevMode(),
      // Register the ServiceWorker as soon as the application is stable
      // or after 30 seconds (whichever comes first).
      registrationStrategy: 'registerWhenStable:30000'
    }),
  ],
  providers: [httpInterceptorProviders],
  bootstrap: [AppComponent]
})
export class AppModule { }
