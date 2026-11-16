import { computed, inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { LoginRequest } from '../api/api.models';
import { ApiService } from '../api/api.service';
import { tap } from 'rxjs';
import { AuthStore } from './auth.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly state = inject(AuthStore);
  readonly isAuthenticated = computed(() => this.state.accessToken() !== null);
  readonly accountType = computed(() => this.state.accountType());
  readonly mustChangePassword = computed(() => this.state.mustChangePassword());
  login(request: LoginRequest) { return this.api.login(request).pipe(tap(response => this.state.setToken(response.accessToken, response.accountType, response.mustChangePassword))); }
  changePassword(request: { currentPassword: string; newPassword: string }) { return this.api.changePassword(request).pipe(tap(() => this.state.completePasswordChange())); }
  getToken() { return this.state.accessToken(); }
  logout() { this.state.clearToken(); void this.router.navigate(['/login']); }
}
