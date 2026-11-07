import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { DemoBanner } from "./DemoBanner";

function renderBanner() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <DemoBanner />
    </QueryClientProvider>,
  );
}

function statusResponse(showDemoBanner: boolean, dataMode: string, dataNotice: string) {
  return new Response(JSON.stringify({ dataMode, showDemoBanner, dataNotice, disclaimer: "d", serverTimeUtc: "2026-11-07T04:00:00Z" }), {
    status: 200,
    headers: { "content-type": "application/json" },
  });
}

describe("DemoBanner", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("shows the server's DEMO notice when no licensed provider is configured", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(statusResponse(true, "Simulated", "DEMO: all market data is SIMULATED")));
    renderBanner();
    expect(await screen.findByText(/all market data is SIMULATED/)).toBeInTheDocument();
  });

  it("is visible while the status is still loading (fail-safe)", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise<Response>(() => {})));
    renderBanner();
    expect(screen.getByTestId("demo-banner")).toHaveTextContent(/DEMO/);
  });

  it("warns that data status is unknown when the server cannot be reached", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("network")));
    renderBanner();
    // The banner is visible throughout; after one retry (~1s) it switches to the "unknown" warning.
    expect(screen.getByTestId("demo-banner")).toBeInTheDocument();
    expect(await screen.findByText(/DATA STATUS UNKNOWN/, undefined, { timeout: 4000 })).toBeInTheDocument();
  });

  it("hides only when the server reports a licensed provider", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(statusResponse(false, "Licensed", "Licensed")));
    renderBanner();
    await waitFor(() => expect(screen.queryByTestId("demo-banner")).not.toBeInTheDocument());
  });
});
