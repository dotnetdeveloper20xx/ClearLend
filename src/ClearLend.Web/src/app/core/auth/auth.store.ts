import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';

type AuthState = { accessToken: string | null };

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState<AuthState>({ accessToken: null }),
  withMethods((store) => ({
    setToken(accessToken: string): void { patchState(store, { accessToken }); },
    clearToken(): void { patchState(store, { accessToken: null }); }
  }))
);
