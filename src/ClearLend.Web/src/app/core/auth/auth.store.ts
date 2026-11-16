import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';

type AuthState = { accessToken: string | null };

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState<AuthState>({ accessToken: typeof sessionStorage === 'undefined' ? null : sessionStorage.getItem('clearlend.accessToken') }),
  withMethods((store) => ({
    setToken(accessToken: string): void { sessionStorage.setItem('clearlend.accessToken', accessToken); patchState(store, { accessToken }); },
    clearToken(): void { sessionStorage.removeItem('clearlend.accessToken'); patchState(store, { accessToken: null }); }
  }))
);
