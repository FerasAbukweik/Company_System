import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { finalize, Observable, shareReplay } from 'rxjs';
import { SKIP_AUTH_RECOVERY } from '../../constants/auth-http-context';
import { Urls } from '../../constants/urls';
import { LoginDTO } from '../../dto/auth/login-dto';
import { UserDTO } from '../../dto/auth/user-dto';

@Injectable({
  providedIn: 'root',
})
export class AuthApiService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private readonly baseUrl = Urls.api + '/Auth';

  // IsAdmin
  public isAdmin() {
    return this.http.post<void>(`${this.baseUrl}/IsAdmin`, {});
  }

  // Login
  public login(loginData: LoginDTO) {
    return this.http.post<UserDTO>(`${this.baseUrl}/Login`, loginData, {
      context: new HttpContext().set(SKIP_AUTH_RECOVERY, true),
    });
  }

  // Logout
  public logout() {
    return this.http.post<void>(
      `${this.baseUrl}/Logout`,
      {},
      {
        context: new HttpContext().set(SKIP_AUTH_RECOVERY, true),
      },
    );
  }

  private refreshRequest: Observable<void> | null = null;

  // Update tokens
  public updateTokens() {
    return (this.refreshRequest ??= this.http
      .post<void>(
        `${this.baseUrl}/UpdateTokens`,
        {},
        {
          context: new HttpContext().set(SKIP_AUTH_RECOVERY, true),
        },
      )
      .pipe(
        finalize(() => (this.refreshRequest = null)),
        shareReplay({ bufferSize: 1, refCount: false }),
      ));
  }
}
