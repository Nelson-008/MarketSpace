import { useSystemStatus } from "../../shared/ui/DemoBanner";
import { Card, Notice } from "../../shared/ui/primitives";
import { formatIst } from "../../shared/format";

/**
 * Market overview. No market data source exists yet, so this page shows that plainly instead of placeholder
 * numbers (spec §18: never fabricate market data; §16.1: no fake live values).
 */
export function HomePage() {
  const status = useSystemStatus();
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Market Overview</h1>
      <Notice>
        No market data is available yet. Indices, breadth, sector performance and gainers/losers will appear here once a
        data provider is configured (simulated data first, in Phase 2, always labelled as SIMULATED).
      </Notice>
      <Card title="Data status">
        <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-3">
          <div>
            <dt className="text-slate-500">Data mode</dt>
            <dd className="font-medium">{status.data?.dataMode ?? "Unknown"}</dd>
          </div>
          <div>
            <dt className="text-slate-500">Server time</dt>
            <dd className="font-medium">{formatIst(status.data?.serverTimeUtc)}</dd>
          </div>
          <div>
            <dt className="text-slate-500">Notice</dt>
            <dd className="font-medium">{status.data?.dataNotice ?? "—"}</dd>
          </div>
        </dl>
      </Card>
    </div>
  );
}

export function PlannedPage({ title, phase, description }: { title: string; phase: string; description: string }) {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">{title}</h1>
      <Card>
        <p className="text-sm text-slate-600 dark:text-slate-300">{description}</p>
        <p className="mt-3 text-xs font-medium uppercase tracking-wide text-slate-500">Planned: {phase}</p>
      </Card>
    </div>
  );
}
