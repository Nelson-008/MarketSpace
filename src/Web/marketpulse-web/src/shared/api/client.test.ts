import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, __resetClientForTests, api, refreshSession, session } from "./client";

function json(status: number, body: unknown) {
  return new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } });
}

const authBody = { accessToken: "new-token", accessTokenExpiresAtUtc: "2026-11-07T04:00:00Z", profile: { displayName: "T" } };

describe("api client", () => {
  beforeEach(() => __resetClientForTests());
  afterEach(() => vi.unstubAllGlobals());

  it("parses the error envelope into ApiError with code and correlation id", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(json(409, { code: "watchlist.duplicate_name", message: "Exists", correlationId: "abc", errors: null })));

    const error = await api("/watchlists", { method: "POST", body: { name: "x" } }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).code).toBe("watchlist.duplicate_name");
    expect((error as ApiError).correlationId).toBe("abc");
    expect((error as ApiError).status).toBe(409);
  });

  it("refreshes once for concurrent 401s (single-flight) and retries with the new token", async () => {
    session.setAccessToken("expired");
    let refreshCalls = 0;
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      if (url.endsWith("/auth/refresh")) {
        refreshCalls++;
        expect((init?.headers as Record<string, string>)["X-Requested-With"]).toBe("MarketPulse");
        return json(200, authBody);
      }
      const auth = (init?.headers as Record<string, string>)["Authorization"];
      return auth === "Bearer new-token" ? json(200, { ok: true }) : json(401, { code: "auth.unauthenticated", message: "no", correlationId: "c" });
    });
    vi.stubGlobal("fetch", fetchMock);

    const results = await Promise.all([api<{ ok: boolean }>("/a"), api<{ ok: boolean }>("/b"), api<{ ok: boolean }>("/c")]);

    expect(results.every((r) => r.ok)).toBe(true);
    expect(refreshCalls).toBe(1);
  });

  it("does not attempt a refresh for anonymous 401s", async () => {
    const fetchMock = vi.fn().mockResolvedValue(json(401, { code: "auth.invalid_credentials", message: "bad", correlationId: "c" }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(api("/auth/login", { method: "POST", body: {} })).rejects.toBeInstanceOf(ApiError);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("notifies listeners and clears the token when refresh fails", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(json(401, { code: "auth.invalid_refresh_token", message: "x", correlationId: "c" })));
    session.setAccessToken("old");
    const listener = vi.fn();
    session.subscribe(listener);

    await expect(refreshSession()).resolves.toBeNull();

    expect(listener).toHaveBeenCalledWith(null);
    expect(session.hasAccessToken()).toBe(false);
  });
});
