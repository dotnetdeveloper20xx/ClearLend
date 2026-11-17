import { DatePipe } from '@angular/common';
import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';

interface DemoBorrowerProfile { givenName: string; familyName: string; email: string; accountStatus: string; profileState: string; consentStatus: string; registrationDate: string; phone: string; businessName: string; location: string; }

@Component({ selector: 'app-borrower-profile', standalone: true, imports: [DatePipe, MatCardModule, MatTabsModule], templateUrl: './borrower-profile.component.html', styleUrl: './borrower-profile.component.scss' })
export class BorrowerProfileComponent {
  readonly profile: DemoBorrowerProfile = { givenName: 'Demo', familyName: 'Borrower', email: 'borrower@clearlend.local', accountStatus: 'Active', profileState: 'Ready for review', consentStatus: 'Granted', registrationDate: '2026-11-14', phone: '+44 20 7946 0123', businessName: 'Greenfield Workshop', location: 'Manchester, UK' };
}
