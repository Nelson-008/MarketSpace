import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { api, refreshSession, session } from "../api/client";
import type { AuthResponse, ProfileResponse } from "../api/types";

type AuthState =
  | { status: "loading"; profile: null }
  | { status: "anonymous"; profile: null }
  | { status: "authenticated"; profile: ProfileResponse };

interface AuthContextValue {
  state: AuthState;
  login(email: string, password: string): Promise<void>;
  logout(): Promise<void>;
  setProfile(profile: ProfileResponse): void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: "loading", profile: null });

  const apply = useCallback((auth: AuthResponse | null) => {
    setState(auth ? { status: "authenticated", profile: auth.profile } : { status: "anonymous", profile: null });
  }, []);

  useEffect(() => {
    const unsubscribe = session.subscribe(apply);
    // Restore the session from the refresh cookie on page load.
    void refreshSession().then(apply);
    return () => {
      unsubscribe();
    };
  }, [apply]);

  const login = useCallback(async (email: string, password: string) => {
    const auth = await api<AuthResponse>("/auth/login", { method: "POST", body: { email, password } });
    session.setAccessToken(auth.accessToken);
    apply(auth);
  }, [apply]);

  const logout = useCallback(async () => {
    try {
      await api<void>("/auth/logout", { method: "POST", csrf: true });
    } finally {
      session.setAccessToken(null);
      apply(null);
    }
  }, [apply]);

  const setProfile = useCallback((profile: ProfileResponse) => setState({ status: "authenticated", profile }), []);

  const value = useMemo(() => ({ state, login, logout, setProfile }), [state, login, logout, setProfile]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>.");
  return ctx;
}
