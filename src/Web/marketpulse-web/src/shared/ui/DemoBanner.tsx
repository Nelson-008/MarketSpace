import { useQuery } from "@tanstack/react-query";
import { api } from "../api/client";
import type { SystemStatus } from "../api/types";

export function useSystemStatus() {
  return useQuery({
    queryKey: ["system-status"],
    queryFn: () => api<SystemStatus>("/system/status"),
    refetchInterval: 60_000,
    retry: 1,
  });
}

/**
 * Persistent, non-dismissible data notice (spec §19: "persistent DEMO/MOCK label until a licensed live provider is
 * configured"). Fail-safe: shown while loading and on error; hidden only when the API reports a licensed provider.
 */
export function DemoBanner() {
  const { data, isError } = useSystemStatus();

  if (data && !data.showDemoBanner) return null;

  const text = isError
    ? "DATA STATUS UNKNOWN — the server could not be reached. Treat all data as unavailable."
    : (data?.dataNotice ?? "DEMO — checking data status…");

  return (
    <div
      role="status"
      aria-live="polite"
      data-testid="demo-banner"
      className="sticky top-0 z-50 border-b border-amber-300 bg-amber-100 px-4 py-2 text-center text-sm font-semibold text-amber-950 dark:border-amber-700 dark:bg-amber-950 dark:text-amber-100"
    >
      {text}
    </div>
  );
}
