import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LoginRequest, LoginResponse, BorrowerProfile, RegisterBorrowerRequest, RegisterBorrowerResponse, RegisterLenderRequest, RegisterLenderResponse, LenderProfile, StaffSummary, CreateStaffRequest, CreateStaffResponse } from './api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1';
  registerBorrower(request: RegisterBorrowerRequest): Observable<RegisterBorrowerResponse> { return this.http.post<RegisterBorrowerResponse>(`${this.baseUrl}/borrowers`, request); }
  registerLender(request: RegisterLenderRequest): Observable<RegisterLenderResponse> { return this.http.post<RegisterLenderResponse>(`${this.baseUrl}/lenders`, request); }
  login(request: LoginRequest): Observable<LoginResponse> { return this.http.post<LoginResponse>(`${this.baseUrl}/auth/login`, request); }
  changePassword(request: { currentPassword: string; newPassword: string }): Observable<void> { return this.http.post<void>(`${this.baseUrl}/auth/change-password`, request); }
  getMyProfile(): Observable<BorrowerProfile> { return this.http.get<BorrowerProfile>(`${this.baseUrl}/me/borrower-profile`); }
  getMyLenderProfile(): Observable<LenderProfile> { return this.http.get<LenderProfile>(`${this.baseUrl}/me/lender-profile`); }
  listStaff(): Observable<StaffSummary[]> { return this.http.get<StaffSummary[]>(`${this.baseUrl}/staff`); }
  createStaff(request: CreateStaffRequest): Observable<CreateStaffResponse> { return this.http.post<CreateStaffResponse>(`${this.baseUrl}/staff`, request); }
  changeStaffStatus(userAccountId: string, restore: boolean): Observable<void> { return this.http.patch<void>(`${this.baseUrl}/staff/${userAccountId}/status`, { restore }); }
  resetStaffPassword(userAccountId: string, newPassword: string): Observable<void> { return this.http.post<void>(`${this.baseUrl}/staff/${userAccountId}/reset-password`, { newPassword }); }
  changeStaffRoles(userAccountId: string, roles: string[]): Observable<void> { return this.http.put<void>(`${this.baseUrl}/staff/${userAccountId}/roles`, { roles }); }
  removeStaff(userAccountId: string): Observable<void> { return this.http.delete<void>(`${this.baseUrl}/staff/${userAccountId}`); }
}
