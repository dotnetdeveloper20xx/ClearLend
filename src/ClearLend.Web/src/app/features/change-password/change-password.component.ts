import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-change-password', standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule],
  template: `<section class="auth-layout"><mat-card><mat-card-header><mat-card-title>Set your private password</mat-card-title><mat-card-subtitle>Your administrator created this account. Choose a password only you know before continuing.</mat-card-subtitle></mat-card-header><mat-card-content><form [formGroup]="form" (ngSubmit)="submit()" novalidate><mat-form-field appearance="outline"><mat-label>Temporary password</mat-label><input matInput type="password" formControlName="currentPassword" autocomplete="current-password"></mat-form-field><mat-form-field appearance="outline"><mat-label>New password</mat-label><input matInput type="password" formControlName="newPassword" autocomplete="new-password"><mat-hint>Use at least 4 characters for this demo.</mat-hint></mat-form-field>@if(error()){<p class="error" role="alert">{{error()}}</p>}<button mat-flat-button color="primary" type="submit" [disabled]="loading()">{{loading()?'Updating…':'Set password and continue'}}</button></form></mat-card-content></mat-card></section>`,
  styles: [`.auth-layout{display:grid;place-items:center;min-height:70vh;padding:2rem 1rem}mat-card{width:min(100%,480px);padding:1rem;border-radius:18px;box-shadow:0 16px 45px #18364a12}mat-card-title{font:400 2rem Georgia,serif;color:#18364a}mat-card-subtitle{margin-top:.5rem;color:#607784;line-height:1.5}form{display:grid;gap:1rem;margin-top:1.5rem}.auth-layout button{min-height:3rem;border-radius:999px}.error{color:#b3261e}`]
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder); private readonly auth = inject(AuthService); private readonly router = inject(Router);
  readonly loading = signal(false); readonly error = signal('');
  readonly form = this.fb.nonNullable.group({ currentPassword: ['', Validators.required], newPassword: ['', [Validators.required, Validators.minLength(4)]] });
  submit(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this.loading.set(true); this.error.set(''); this.auth.changePassword(this.form.getRawValue()).subscribe({ next: () => void this.router.navigate(['/staff']), error: error => { this.error.set(error.error?.message ?? 'The password could not be changed.'); this.loading.set(false); } }); }
}
