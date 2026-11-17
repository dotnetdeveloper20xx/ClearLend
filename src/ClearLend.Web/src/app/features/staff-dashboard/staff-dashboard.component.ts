import { DatePipe } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { StaffSummary } from '../../core/api/api.models';

@Component({ selector: 'app-staff-dashboard', standalone: true, imports: [DatePipe, ReactiveFormsModule, MatCardModule, MatTabsModule], templateUrl: './staff-dashboard.component.html', styleUrl: './staff-dashboard.component.scss' })
export class StaffDashboardComponent {
  readonly Math = Math;
  readonly saving = signal(false); readonly error = signal(''); readonly success = signal('');
  readonly roles = ['OperationsStaff', 'CreditManager'];
  readonly staff = signal<StaffSummary[]>([
    { userAccountId: 'demo-operations-001', email: 'operations@clearlend.local', roles: ['OperationsStaff'], status: 'Active', createdAt: '2026-11-10' },
    { userAccountId: 'demo-credit-001', email: 'credit@clearlend.local', roles: ['CreditManager'], status: 'Active', createdAt: '2026-11-12' },
    { userAccountId: 'demo-dual-001', email: 'review.lead@clearlend.local', roles: ['OperationsStaff', 'CreditManager'], status: 'Disabled', createdAt: '2026-11-14' }
  ]);
  readonly selectedMember = signal<StaffSummary | null>(this.staff()[0]);
  readonly query = signal('');
  readonly statusFilter = signal('All statuses');
  readonly roleFilter = signal('All roles');
  readonly page = signal(1);
  readonly selectedTab = signal(0);
  readonly pageSize = 8;
  readonly filteredStaff = computed(() => {
    const query = this.query().trim().toLowerCase();
    return this.staff().filter(member => {
      const matchesQuery = !query || member.email.toLowerCase().includes(query) || member.roles.some(role => role.toLowerCase().includes(query));
      const matchesStatus = this.statusFilter() === 'All statuses' || member.status === this.statusFilter();
      const matchesRole = this.roleFilter() === 'All roles' || member.roles.includes(this.roleFilter());
      return matchesQuery && matchesStatus && matchesRole;
    });
  });
  readonly pagedStaff = computed(() => this.filteredStaff().slice((this.page() - 1) * this.pageSize, this.page() * this.pageSize));
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.filteredStaff().length / this.pageSize)));
  readonly form = new FormGroup({ email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }), password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(4)] }), roles: new FormControl<string[]>([], { nonNullable: true, validators: [Validators.required] }) });

  toggleRole(role: string): void { const selected = this.form.controls.roles.value; this.form.controls.roles.setValue(selected.includes(role) ? selected.filter(item => item !== role) : [...selected, role]); this.form.controls.roles.markAsTouched(); }
  setQuery(event: Event): void { this.query.set((event.target as HTMLInputElement).value); this.page.set(1); }
  setStatusFilter(event: Event): void { this.statusFilter.set((event.target as HTMLSelectElement).value); this.page.set(1); }
  setRoleFilter(event: Event): void { this.roleFilter.set((event.target as HTMLSelectElement).value); this.page.set(1); }
  previousPage(): void { this.page.update(value => Math.max(1, value - 1)); }
  nextPage(): void { this.page.update(value => Math.min(this.pageCount(), value + 1)); }
  create(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this.saving.set(true); const value = this.form.getRawValue(); const member: StaffSummary = { userAccountId: `demo-${Date.now()}`, email: value.email, roles: value.roles, status: 'Active', createdAt: new Date().toISOString() }; this.staff.update(items => [...items, member]); this.selectedMember.set(member); this.success.set('Demo staff account added. No real account was created.'); this.form.reset({ email: '', password: '', roles: [] }); this.saving.set(false); }
  select(member: StaffSummary): void { this.selectedMember.set(member); this.selectedTab.set(2); this.error.set(''); this.success.set(''); }
  changeStatus(member: StaffSummary): void { const status = member.status === 'Active' ? 'Disabled' : 'Active'; this.updateMember(member, { status }); this.success.set(`Demo access ${status === 'Active' ? 'restored' : 'suspended'} for ${member.email}.`); }
  resetPassword(member: StaffSummary): void { this.success.set(`Demo password reset recorded for ${member.email}. A real reset would send a secure workflow.`); }
  changeRoles(member: StaffSummary): void { const roles = member.roles.includes('CreditManager') ? ['OperationsStaff'] : ['OperationsStaff', 'CreditManager']; this.updateMember(member, { roles }); this.success.set(`Demo roles updated for ${member.email}.`); }
  remove(member: StaffSummary): void { this.updateMember(member, { status: 'Removed' }); this.success.set(`Demo access removed for ${member.email}. Historical activity remains available.`); }
  private updateMember(member: StaffSummary, changes: Partial<StaffSummary>): void { this.staff.update(items => items.map(item => item.userAccountId === member.userAccountId ? { ...item, ...changes } : item)); this.selectedMember.set({ ...member, ...changes }); }
}
