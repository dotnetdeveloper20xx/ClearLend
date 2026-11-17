import { Routes } from '@angular/router';
import { LoginComponent } from './features/login/login.component';
import { ChangePasswordComponent } from './features/change-password/change-password.component';
import { BorrowerProfileComponent } from './features/borrower-profile/borrower-profile.component';
import { BorrowerRegistrationComponent } from './features/borrower-registration/borrower-registration.component';
import { LenderRegistrationComponent } from './features/lender-registration/lender-registration.component';
import { LenderProfileComponent } from './features/lender-profile/lender-profile.component';
import { WelcomeComponent } from './features/welcome/welcome.component';
import { StaffDashboardComponent } from './features/staff-dashboard/staff-dashboard.component';
import { HowItWorksComponent } from './features/how-it-works/how-it-works.component';
import { BorrowerDashboardComponent } from './features/borrower-dashboard/borrower-dashboard.component';
import { LenderDashboardComponent } from './features/lender-dashboard/lender-dashboard.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'welcome' },
  { path: 'welcome', component: WelcomeComponent },
  { path: 'how-it-works', component: HowItWorksComponent },
  { path: 'borrower', component: BorrowerDashboardComponent },
  { path: 'lender', component: LenderDashboardComponent },
  { path: 'register', component: BorrowerRegistrationComponent },
  { path: 'lender/register', component: LenderRegistrationComponent },
  { path: 'login', component: LoginComponent },
  { path: 'change-password', component: ChangePasswordComponent },
  { path: 'profile', component: BorrowerProfileComponent },
  { path: 'lender/profile', component: LenderProfileComponent },
  { path: 'staff', component: StaffDashboardComponent },
  { path: '**', redirectTo: 'welcome' }
];
