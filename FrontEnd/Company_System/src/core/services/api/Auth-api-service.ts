import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Urls } from '../../constants/urls';
import { AccountCreateDTO } from '../../dto/create-account-dto';
import { LoginDTO } from '../../dto/login-dto';
import { AuthDTO } from '../../dto/auth-dto';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private readonly baseUrl = Urls.api + '/Auth';
  private userData: AuthDTO | null = null;

  // getters
  get getUserData(){
    return this.userData;
  }

  // IsAuthenticated
  public isAuthenticated() {
    return this.http.post<AuthDTO>(`${this.baseUrl}/IsAuthenticated`, {})
    .pipe(tap((authData) => {
      this.userData = authData;
    }));
  }

  // IsAdmin
  public isAdmin() {
    return this.http.post<void>(`${this.baseUrl}/IsAdmin`, {});
  }

  // Signup
  public signup(accountData: AccountCreateDTO) {
    return this.http.post<void>(`${this.baseUrl}/Signup`, accountData);
  }

  // Login
  public login(loginData: LoginDTO) {
    return this.http.post<void>(`${this.baseUrl}/Login`, loginData);
  }

  // Logout
  public logout() {
    this.userData = null;

    return this.http.post<void>(`${this.baseUrl}/Logout`, {});
  }

  // Update tokens
  public UpdateTokens() {
    return this.http.post<void>(`${this.baseUrl}/UpdateTokens`, {});
  }
}
