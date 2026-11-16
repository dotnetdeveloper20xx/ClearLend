import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../core/api/api.service';
import { StaffSummary } from '../../core/api/api.models';

@Component({
  selector: 'app-staff-dashboard', standalone: true, imports: [DatePipe, ReactiveFormsModule, MatCardModule, MatProgressSpinnerModule],
  templateUrl: './staff-dashboard.component.html', styleUrl: './staff-dashboard.component.scss'
})
export class StaffDashboardComponent {
  private readonly api = inject(ApiService);
  readonly loading = signal(true); readonly saving = signal(false); readonly error = signal(''); readonly success = signal(''); readonly staff = signal<StaffSummary[]>([]);
  readonly roles = ['OperationsStaff', 'CreditManager'];
  readonly form = new FormGroup({ email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }), password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(4)] }), roles: new FormControl<string[]>([], { nonNullable: true, validators: [Validators.required] }) });
  constructor() { this.load(); }
  load(): void { this.api.listStaff().subscribe({ next: staff => { this.staff.set(staff); this.loading.set(false); }, error: (error: HttpErrorResponse) => { this.error.set(error.error?.message ?? `We could not load staff (HTTP ${error.status}).`); this.loading.set(false); } }); }
  toggleRole(role: string): void { const selected = this.form.controls.roles.value; this.form.controls.roles.setValue(selected.includes(role) ? selected.filter(x => x !== role) : [...selected, role]); this.form.controls.roles.markAsTouched(); }
  create(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this.saving.set(true); this.error.set(''); this.success.set(''); this.api.createStaff(this.form.getRawValue()).subscribe({ next: () => { this.success.set('Staff account created. The staff member must change the temporary password at first login.'); this.form.reset({ email: '', password: '', roles: [] }); this.saving.set(false); this.load(); }, error: (error: HttpErrorResponse) => { this.error.set(error.error?.message ?? `The staff account could not be created (HTTP ${error.status}).`); this.saving.set(false); } }); }
  changeStatus(member: StaffSummary): void { this.api.changeStaffStatus(member.userAccountId, member.status !== 'Active').subscribe({ next: () => { this.success.set(member.status === 'Active' ? 'Staff access suspended.' : 'Staff access restored.'); this.load(); }, error: (error: HttpErrorResponse) => this.error.set(error.error?.message ?? `Staff status could not be changed (HTTP ${error.status}).`) }); }
  resetPassword(member: StaffSummary): void { const password = window.prompt(`Enter a temporary password for ${member.email}:`); if (!password) return; this.api.resetStaffPassword(member.userAccountId, password).subscribe({ next: () => this.success.set('Password reset. The staff member must change it at next login.'), error: (error: HttpErrorResponse) => this.error.set(error.error?.message ?? `Password could not be reset (HTTP ${error.status}).`) }); }
  changeRoles(member: StaffSummary): void { const value = window.prompt('Enter roles separated by commas: OperationsStaff, CreditManager', member.roles.join(', ')); if (!value) return; const roles = value.split(',').map(role => role.trim()).filter(Boolean); this.api.changeStaffRoles(member.userAccountId, roles).subscribe({ next: () => { this.success.set('Staff roles updated.'); this.load(); }, error: (error: HttpErrorResponse) => this.error.set(error.error?.message ?? `Roles could not be updated (HTTP ${error.status}).`) }); }
  remove(member: StaffSummary): void { if (!window.confirm(`Remove access for ${member.email}? This preserves history and cannot be undone.`)) return; this.api.removeStaff(member.userAccountId).subscribe({ next: () => { this.success.set('Staff access permanently removed.'); this.load(); }, error: (error: HttpErrorResponse) => this.error.set(error.error?.message ?? `Staff access could not be removed (HTTP ${error.status}).`) }); }
}
