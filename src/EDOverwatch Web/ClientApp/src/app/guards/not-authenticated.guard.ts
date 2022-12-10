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
    const authenticationStatus = await this.websocketService.authenticationResolved;
    if (authenticationStatus === ConnectionStatus.NotAuthenticated) {
      return true;
    }
    this.router.navigate(["/"]);
    return false;
  }
}
