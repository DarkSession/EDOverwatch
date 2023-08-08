import { RouteReuseStrategy, UrlSegment } from '@angular/router/';
import { ActivatedRouteSnapshot, DetachedRouteHandle } from '@angular/router';
import { environment } from 'src/environments/environment';
export class AppRouteReuseStrategy implements RouteReuseStrategy {

    private storedRoutes: { [key: string]: RouteStorageObject } = {};

    public shouldDetach(route: ActivatedRouteSnapshot): boolean {
        return route.data.reuseRoute || false;
    }

    public store(route: ActivatedRouteSnapshot, handle: DetachedRouteHandle): void {
        const id = this.createIdentifier(route);
        if (route.data.reuseRoute && id.length > 0) {
            this.storedRoutes[id] = { handle, snapshot: route };
        }
    }

    public shouldAttach(route: ActivatedRouteSnapshot): boolean {
        const id = this.createIdentifier(route);
        const storedObject = this.storedRoutes[id];
        const canAttach = !!route.routeConfig && !!storedObject;
        if (!canAttach) return false;

        const paramsMatch = this.compareObjects(route.params, storedObject.snapshot.params);
        const queryParamsMatch = this.compareObjects(route.queryParams, storedObject.snapshot.queryParams);

        if (!environment.production) {
            console.log('deciding to attach...', route, 'does it match?');
            console.log('param comparison:', paramsMatch);
            console.log('query param comparison', queryParamsMatch);
            console.log(storedObject.snapshot, 'return: ', paramsMatch && queryParamsMatch);
        }

        return paramsMatch && queryParamsMatch;
    }

    public retrieve(route: ActivatedRouteSnapshot): DetachedRouteHandle | null {
        const id = this.createIdentifier(route);
        if (!route.routeConfig || !this.storedRoutes[id]) {
            return null;
        }
        return this.storedRoutes[id].handle;
    }

    public shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
        return future.routeConfig === curr.routeConfig;
    }

    private createIdentifier(route: ActivatedRouteSnapshot) {
        // Build the complete path from the root to the input route
        const segments: UrlSegment[][] = route.pathFromRoot.map(r => r.url);
        const subpaths = ([] as UrlSegment[]).concat(...segments).map(segment => segment.path);
        // Result: ${route_depth}-${path}
        return segments.length + '-' + subpaths.join('/');
    }

    private compareObjects(base: any, compare: any): boolean {

        // loop through all properties
        for (const baseProperty in { ...base, ...compare }) {

            // determine if comparrison object has that property, if not: return false
            if (compare.hasOwnProperty(baseProperty)) {
                switch (typeof base[baseProperty]) {
                    // if one is object and other is not: return false
                    // if they are both objects, recursively call this comparison function
                    case 'object':
                        if (typeof compare[baseProperty] !== 'object' || !this.compareObjects(base[baseProperty], compare[baseProperty])) {
                            return false;
                        }
                        break;
                    // if one is function and other is not: return false
                    // if both are functions, compare function.toString() results
                    case 'function':
                        if (typeof compare[baseProperty] !== 'function' || base[baseProperty].toString() !== compare[baseProperty].toString()) {
                            return false;
                        }
                        break;
                    // otherwise, see if they are equal using coercive comparison
                    default:
                        // tslint:disable-next-line triple-equals
                        if (base[baseProperty] != compare[baseProperty]) {
                            return false;
                        }
                }
            } else {
                return false;
            }
        }

        // returns true only after false HAS NOT BEEN returned through all loops
        return true;
    }
}

interface RouteStorageObject {
    snapshot: ActivatedRouteSnapshot;
    handle: DetachedRouteHandle;
}