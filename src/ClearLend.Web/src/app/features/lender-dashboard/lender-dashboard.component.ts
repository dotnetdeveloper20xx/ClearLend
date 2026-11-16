import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
@Component({ selector: 'app-lender-dashboard', standalone: true, imports: [RouterLink, MatCardModule], templateUrl: './lender-dashboard.component.html', styleUrl: './lender-dashboard.component.scss' })
export class LenderDashboardComponent { readonly opportunities = [{ name: 'Demo sustainable expansion', sector: 'Renewable services', amount: '£75,000' }, { name: 'Demo equipment purchase', sector: 'Manufacturing', amount: '£42,500' }]; }
