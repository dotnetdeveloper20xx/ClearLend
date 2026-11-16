import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
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
  { path: 'borrower', component: BorrowerDashboardComponent, canActivate: [authGuard] },
  { path: 'lender', component: LenderDashboardComponent, canActivate: [authGuard] },
  { path: 'register', component: BorrowerRegistrationComponent },
  { path: 'lender/register', component: LenderRegistrationComponent },
  { path: 'login', component: LoginComponent },
  { path: 'change-password', component: ChangePasswordComponent, canActivate: [authGuard] },
  { path: 'profile', component: BorrowerProfileComponent, canActivate: [authGuard] },
  { path: 'lender/profile', component: LenderProfileComponent, canActivate: [authGuard] },
  { path: 'staff', component: StaffDashboardComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'welcome' }
];
