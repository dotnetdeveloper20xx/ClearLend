import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { ApiService } from '../../core/api/api.service';

@Component({ selector: 'app-lender-registration', standalone: true, imports: [ReactiveFormsModule, RouterLink, MatCardModule], styleUrl: './lender-registration.component.scss', templateUrl: './lender-registration.component.html' })
export class LenderRegistrationComponent {
  private readonly fb = inject(FormBuilder); private readonly api = inject(ApiService);
  readonly message = signal('');
  readonly form = this.fb.nonNullable.group({ legalBusinessName: ['', Validators.required], tradingName: [''], contactGivenName: ['', Validators.required], contactFamilyName: ['', Validators.required], email: ['', [Validators.required, Validators.email]], password: ['', [Validators.required, Validators.minLength(4)]], consent: [false, Validators.requiredTrue] });
  submit(): void { if (this.form.invalid) { this.form.markAllAsTouched(); this.message.set('Please complete all required fields and accept the lender terms.'); return; } const value = this.form.getRawValue(); this.api.registerLender({ ...value, consentPolicyVersion: 'lender-demo-v1' }).subscribe({ next: () => this.message.set('Your lender account has been created. You can now sign in.'), error: error => this.message.set(error.error?.message ?? `Registration failed (HTTP ${error.status}).`) }); }
}
