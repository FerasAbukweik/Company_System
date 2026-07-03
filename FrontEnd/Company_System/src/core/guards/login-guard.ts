import { inject } from "@angular/core";
import { CanMatchFn, Router } from "@angular/router";
import { AuthService } from "../services/api/AuthService";
import { take } from "rxjs";

export const loginGuard: CanMatchFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);


    const sub = authService.isAuthenticated()
    .pipe(take(1))
    .subscribe({
        next: () => {
            router.navigateByUrl("/");
        },
    });


    return true;
}