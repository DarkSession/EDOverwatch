import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, CanActivateChild, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { ConnectionStatus, WebsocketService } from '../services/websocket.service';

@Injectable({
  providedIn: 'root'
})
export class AuthenticationGuard implements CanActivate, CanActivateChild {
  public constructor(
    private readonly websocketService: WebsocketService,
    private readonly router: Router
  ) {
  }

  public async canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot,
  ): Promise<boolean | UrlTree> {
    await this.websocketService.authenticationResolved;
    if (this.websocketService.connectionStatus === ConnectionStatus.Open && this.websocketService.connectionIsAuthenticated) {
      return true;
    }
    this.router.navigate(["/login"]);
    return false;
  }

  public canActivateChild(childRoute: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    return this.canActivate(childRoute, state);
  }
}
