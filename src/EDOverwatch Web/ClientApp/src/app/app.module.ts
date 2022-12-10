/* "Barrel" of Http Interceptors */
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { HttpRequestInterceptor } from './services/HttpRequestInterceptor';

import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
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

    RouterModule.forRoot([
      {
        path: 'systems',
        component: SystemsComponent
      },
      {
        path: 'about',
        component: AboutComponent
      },
      {
        path: 'get-involved',
        component: GetInvolvedComponent
      },
      {
        path: 'consumer-api',
        component: ConsumerApiComponent,
      },
      {
        path: 'login',
        component: LoginComponent,
        canActivate: [NotAuthenticatedGuard],
      },
      {
        path: 'auth',
        component: AuthComponent,
        canActivate: [NotAuthenticatedGuard],
      },
      {
        path: 'my-efforts',
        component: MyEffortsComponent,
        canActivate: [AuthenticationGuard],
      },
      {
        path: '**',
        component: HomeComponent
      },
    ]),
  ],
  providers: [httpInterceptorProviders],
  bootstrap: [AppComponent]
})
export class AppModule { }
