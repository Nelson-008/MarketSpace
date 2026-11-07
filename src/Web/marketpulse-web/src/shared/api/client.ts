import type { ApiErrorBody, AuthResponse } from "./types";

export const API_BASE = "/api/v1";
const CSRF_HEADER = { "X-Requested-With": "MarketPulse" } as const;

/** An error returned by the API in the standard envelope (code, message, correlationId, errors). */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly correlationId: string | null;
  readonly fieldErrors: Record<string, string[]>;

  constructor(status: number, body: Partial<ApiErrorBody> | null) {
    super(body?.message ?? `Request failed (${status}).`);
    this.name = "ApiError";
    this.status = status;
    this.code = body?.code ?? "request.failed";
    this.correlationId = body?.correlationId ?? null;
    this.fieldErrors = (body?.errors as Record<string, string[]> | null | undefined) ?? {};
  }
}

// The access token lives only in memory (ADR-0004). The refresh token is an HttpOnly cookie the script cannot read.
let accessToken: string | null = null;
let refreshInFlight: Promise<AuthResponse | null> | null = null;
const sessionListeners = new Set<(session: AuthResponse | null) => void>();

export const session = {
  setAccessToken(token: string | null) {
    accessToken = token;
  },
  hasAccessToken: () => accessToken !== null,
  /** Notified when a refresh succeeds or the session is lost. */
  subscribe(listener: (session: AuthResponse | null) => void) {
    sessionListeners.add(listener);
    return () => sessionListeners.delete(listener);
  },
};

/**
 * Exchanges the refresh cookie for a new access token. Single-flight: concurrent callers share one request,
 * because presenting the same rotated refresh token twice is treated as theft and revokes the session.
 */
export function refreshSession(): Promise<AuthResponse | null> {
  refreshInFlight ??= (async () => {
    try {
      const response = await fetch(`${API_BASE}/auth/refresh`, {
        method: "POST",
        credentials: "same-origin",
        headers: CSRF_HEADER,
      });
      const auth = response.ok ? ((await response.json()) as AuthResponse) : null;
      accessToken = auth?.accessToken ?? null;
      sessionListeners.forEach((l) => l(auth));
      return auth;
    } catch {
      accessToken = null;
      sessionListeners.forEach((l) => l(null));
      return null;
    } finally {
      refreshInFlight = null;
    }
  })();
  return refreshInFlight;
}

export interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  /** Send the anti-forgery header (cookie-authenticated auth endpoints). */
  csrf?: boolean;
  signal?: AbortSignal;
}

/** Calls the API. On 401 with an access token it refreshes once and retries. Throws {@link ApiError}. */
export async function api<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const send = () => {
    const headers: Record<string, string> = { Accept: "application/json" };
    if (options.body !== undefined) headers["Content-Type"] = "application/json";
    if (accessToken) headers["Authorization"] = `Bearer ${accessToken}`;
    if (options.csrf) Object.assign(headers, CSRF_HEADER);
    return fetch(`${API_BASE}${path}`, {
      method: options.method ?? "GET",
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      credentials: "same-origin",
      signal: options.signal,
    });
  };

  let response = await send();
  if (response.status === 401 && accessToken !== null) {
    const refreshed = await refreshSession();
    if (refreshed) response = await send();
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readJson<ApiErrorBody>(response));
  }

  return (await readJson<T>(response)) as T;
}

async function readJson<T>(response: Response): Promise<T | null> {
  if (response.status === 204) return null;
  const type = response.headers.get("content-type") ?? "";
  if (!type.includes("json")) return null;
  try {
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

/** Resets module state between tests. */
export function __resetClientForTests() {
  accessToken = null;
  refreshInFlight = null;
  sessionListeners.clear();
}
