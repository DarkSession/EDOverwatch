import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { ConnectionStatus, WebsocketService } from '../services/websocket.service';

@Injectable({
  providedIn: 'root'
})
export class NotAuthenticatedGuard implements CanActivate {
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
    if (this.websocketService.connectionStatus === ConnectionStatus.Open && !this.websocketService.connectionIsAuthenticated) {
      return true;
    }
    this.router.navigate(["/"]);
    return false;
  }
}
