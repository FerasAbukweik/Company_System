import { inject } from "@angular/core";
import { CanMatchFn, Router } from "@angular/router";
import { AuthService } from "../services/api/AuthService";
import { take } from "rxjs";

export const globalGuard: CanMatchFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);


    const sub = authService.isAuthenticated()
    .pipe(take(1))
    .subscribe({
        error: () => {
            router.navigateByUrl("/login");
            
            sub.unsubscribe();
        }
    })


    return true;
}