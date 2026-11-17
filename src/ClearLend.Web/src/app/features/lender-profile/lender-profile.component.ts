import { DatePipe } from '@angular/common';
import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';

interface DemoLenderProfile { legalBusinessName: string; tradingName: string; email: string; contactName: string; accountStatus: string; profileState: string; consentStatus: string; registrationDate: string; registrationNumber: string; sector: string; }

@Component({ selector: 'app-lender-profile', standalone: true, imports: [DatePipe, MatCardModule, MatTabsModule], templateUrl: './lender-profile.component.html', styleUrl: './lender-profile.component.scss' })
export class LenderProfileComponent { readonly profile: DemoLenderProfile = { legalBusinessName: 'ClearLend Demo Lending', tradingName: 'ClearLend Capital', email: 'lender@clearlend.local', contactName: 'Demo Lender', accountStatus: 'Active', profileState: 'Profile complete', consentStatus: 'Granted', registrationDate: '2026-11-14', registrationNumber: 'DEMO-001', sector: 'Business finance' }; }
