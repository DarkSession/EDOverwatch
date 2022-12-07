import { Injectable } from '@angular/core';
import {
    HttpRequest,
    HttpHandler,
    HttpEvent,
    HttpInterceptor, HttpResponse
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError, map } from 'rxjs/operators'
import { AppService } from './app.service';

/**
 * This class is for intercepting http requests. When a request starts, we set the loadingSub property
 * in the LoadingService to true. Once the request completes and we have a response, set the loadingSub
 * property to false. If an error occurs while servicing the request, set the loadingSub property to false.
 * @class {HttpRequestInterceptor}
 */
@Injectable()
export class HttpRequestInterceptor implements HttpInterceptor {

    public constructor(
        private readonly appService: AppService
    ) { }

    public intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        this.appService.setLoading(true, request.url);
        return next.handle(request)
            .pipe(catchError((err) => {
                this.appService.setLoading(false, request.url);
                return err;
            }))
            .pipe(map<unknown, any>((evt: unknown) => {
                if (evt instanceof HttpResponse) {
                    this.appService.setLoading(false, request.url);
                }
                return evt;
            }));
    }
}