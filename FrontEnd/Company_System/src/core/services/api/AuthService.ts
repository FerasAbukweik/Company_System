import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiUrls } from '../../constants/urls'; 
import { AccountCreateDTO } from '../../dto/auth/create-account-dto';
import { LoginDTO } from '../../dto/auth/login-dto';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private readonly baseUrl = ApiUrls.api + '/Auth';


  // IsAuthenticated
  public isAuthenticated() {
    return this.http.post<void>(`${this.baseUrl}/IsAuthenticated`, {});
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

  // Update tokens
  public UpdateTokens(){
    return this.http.post<void>(`${this.baseUrl}/UpdateTokens`, {});
  }
}