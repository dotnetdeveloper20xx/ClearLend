import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';

type AuthState = { accessToken: string | null; accountType: 'Borrower' | 'Lender' | 'Staff' | null; mustChangePassword: boolean };

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState<AuthState>({ accessToken: typeof sessionStorage === 'undefined' ? null : sessionStorage.getItem('clearlend.accessToken'), accountType: typeof sessionStorage === 'undefined' ? null : sessionStorage.getItem('clearlend.accountType') as AuthState['accountType'], mustChangePassword: typeof sessionStorage !== 'undefined' && sessionStorage.getItem('clearlend.mustChangePassword') === 'true' }),
  withMethods((store) => ({
    setToken(accessToken: string, accountType: AuthState['accountType'], mustChangePassword: boolean): void { sessionStorage.setItem('clearlend.accessToken', accessToken); sessionStorage.setItem('clearlend.accountType', accountType ?? ''); sessionStorage.setItem('clearlend.mustChangePassword', String(mustChangePassword)); patchState(store, { accessToken, accountType, mustChangePassword }); },
    completePasswordChange(): void { sessionStorage.setItem('clearlend.mustChangePassword', 'false'); patchState(store, { mustChangePassword: false }); },
    clearToken(): void { sessionStorage.removeItem('clearlend.accessToken'); sessionStorage.removeItem('clearlend.accountType'); sessionStorage.removeItem('clearlend.mustChangePassword'); patchState(store, { accessToken: null, accountType: null, mustChangePassword: false }); }
  }))
);
