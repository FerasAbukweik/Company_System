import { HttpHandlerFn, HttpInterceptorFn, HttpRequest } from "@angular/common/http";
import { inject } from "@angular/core";
import { Router } from "@angular/router";
import { catchError, switchMap, throwError } from "rxjs";
import { AuthService } from "../services/api/AuthService"; 

export const authInterceptor: HttpInterceptorFn = 
(req: HttpRequest<unknown>, next: HttpHandlerFn) => {
    const reqClone = req.clone({
        withCredentials: true,
        headers: req.headers.set('Content-Type', 'application/json')
    });

    const router = inject(Router);
    const authService = inject(AuthService);

    return next(reqClone).pipe(
        catchError((err) => {
            if (err.status === 401 && !router.url.includes('login')) {
                return authService.UpdateTokens().pipe(
                    switchMap(() => {
                        return next(reqClone);
                    }),
                    catchError((refreshErr) => {
                        router.navigateByUrl('/login');
                        return throwError(() => refreshErr);
                    })
                );
            }

            return throwError(() => err);
        })
    );
}