import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

interface AuthResponse {
  token: string;
}

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

  isLoggedIn(): boolean {
    return !!this.getToken();
  }


  getUserId(): string | null {
    const token = this.getToken();
    if (!token) return null;

    try {
      const payload = token.split('.')[1];
      const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
      return decoded['nameid'] ?? decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? null;
    } catch {
      return null;
    }
  }

  getProfileStorageKey(): string {
    const userId = this.getUserId();
    return userId ? `candidate_profile_${userId}` : 'candidate_profile_anonymous';
  }

  private saveToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }
}
