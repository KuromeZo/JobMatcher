import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

interface AuthResponse {
  token: string;
}

// ASP.NET writes ClaimTypes.NameIdentifier under its full URI unless claim mapping is turned off
const NAME_ID_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private readonly TOKEN_KEY = 'jwt_token';
  private readonly baseUrl = `${environment.apiUrl}/api/auth`;

  register(login: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, { login, password }).pipe(
      tap(res => this.saveToken(res.token))
    );
  }

  login(login: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, { login, password }).pipe(
      tap(res => this.saveToken(res.token))
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /** True only for a token that has not expired yet, so the guard doesn't let in a dead session. */
  isLoggedIn(): boolean {
    const exp = this.decodePayload()?.['exp'];
    return typeof exp === 'number' && exp * 1000 > Date.now();
  }

  getUserId(): string | null {
    const payload = this.decodePayload();
    const id = payload?.['nameid'] ?? payload?.[NAME_ID_CLAIM];
    return typeof id === 'string' ? id : null;
  }

  private decodePayload(): Record<string, unknown> | null {
    const token = this.getToken();
    if (!token) return null;

    try {
      const payload = token.split('.')[1];
      return JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
    } catch {
      return null;
    }
  }

  private saveToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }
}
