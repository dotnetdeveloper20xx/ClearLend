import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
@Component({ selector: 'app-borrower-dashboard', standalone: true, imports: [RouterLink, MatCardModule], templateUrl: './borrower-dashboard.component.html', styleUrl: './borrower-dashboard.component.scss' })
export class BorrowerDashboardComponent { readonly applications = [{ name: 'Demo growth facility', status: 'Preparing', amount: '£25,000' }, { name: 'Demo equipment finance', status: 'Draft', amount: '£12,500' }]; }
