import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Urls } from '../../constants/urls';
import { AddEmployeeDTO } from '../../dto/create-account-dto';
import { LoginDTO } from '../../dto/login-dto';
import { UserDTO } from '../../dto/user-dto';

@Injectable({
  providedIn: 'root',
})
export class AuthApiService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private readonly baseUrl = Urls.api + '/Auth';

  // IsAuthenticated
  public isAuthenticated() {
    return this.http.post<void>(`${this.baseUrl}/IsAuthenticated`, {});
  }

  // IsAdmin
  public isAdmin() {
    return this.http.post<void>(`${this.baseUrl}/IsAdmin`, {});
  }

  // Signup
  public signup(accountData: AddEmployeeDTO) {
    return this.http.post<void>(`${this.baseUrl}/Signup`, accountData);
  }

  // Login
  public login(loginData: LoginDTO) {
    return this.http.post<UserDTO>(`${this.baseUrl}/Login`, loginData);
  }

  // Logout
  public logout() {
    return this.http.post<void>(`${this.baseUrl}/Logout`, {});
  }

  // Update tokens
  public UpdateTokens() {
    return this.http.post<void>(`${this.baseUrl}/UpdateTokens`, {});
  }
}
