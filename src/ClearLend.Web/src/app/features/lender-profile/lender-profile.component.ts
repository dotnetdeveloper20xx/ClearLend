import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../core/api/api.service';
import { LenderProfile } from '../../core/api/api.models';

@Component({
  selector: 'app-lender-profile',
  standalone: true,
  imports: [DatePipe, MatCardModule, MatProgressSpinnerModule],
  templateUrl: './lender-profile.component.html',
  styleUrl: './lender-profile.component.scss'
})
export class LenderProfileComponent {
  private readonly api = inject(ApiService);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly profile = signal<LenderProfile | null>(null);

  constructor() {
    this.api.getMyLenderProfile().subscribe({
      next: profile => {
        this.profile.set(profile);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        const apiMessage = error.error?.message;
        this.error.set(apiMessage ? `${apiMessage} (HTTP ${error.status})` : `We could not load your lender profile (HTTP ${error.status}).`);
        this.loading.set(false);
      }
    });
  }
}
