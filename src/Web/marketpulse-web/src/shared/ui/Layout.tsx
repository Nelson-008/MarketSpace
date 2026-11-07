import { useQuery } from "@tanstack/react-query";
import { NavLink, Outlet } from "react-router";
import { api } from "../api/client";
import type { Preferences } from "../api/types";
import { useAuth } from "../auth/AuthProvider";
import { DemoBanner, useSystemStatus } from "./DemoBanner";

interface NavItem {
  to: string;
  label: string;
  /** Shown in Explore (guided) mode as well as Pro. */
  explore: boolean;
}

// Spec §9 information architecture. Sections not yet built show a "planned" page; nothing is faked.
export const NAV: NavItem[] = [
  { to: "/", label: "Market Overview", explore: true },
  { to: "/discover", label: "Discover Stocks", explore: true },
  { to: "/institutional", label: "Institutional", explore: false },
  { to: "/scanner", label: "Scanner", explore: false },
  { to: "/research", label: "Stock Research", explore: true },
  { to: "/ai-analyst", label: "AI Analyst", explore: true },
  { to: "/terminal", label: "Trading Terminal", explore: false },
  { to: "/fno", label: "F&O", explore: false },
  { to: "/strategy-lab", label: "Strategy Lab", explore: false },
  { to: "/paper-trading", label: "Paper Trading", explore: true },
  { to: "/watchlists", label: "Watchlists", explore: true },
  { to: "/settings", label: "Settings", explore: true },
];

export function usePreferences() {
  const { state } = useAuth();
  return useQuery({
    queryKey: ["preferences"],
    queryFn: () => api<Preferences>("/preferences"),
    enabled: state.status === "authenticated",
  });
}

export function Layout() {
  const { state, logout } = useAuth();
  const prefs = usePreferences();
  const status = useSystemStatus();
  const mode = prefs.data?.experienceMode ?? "Explore";
  const items = NAV.filter((n) => mode === "Pro" || n.explore);

  return (
    <div className="flex min-h-screen flex-col">
      <DemoBanner />
      <header className="border-b border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-3">
          <span className="text-lg font-bold tracking-tight">MarketPulse</span>
          <span className="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600 dark:bg-slate-800 dark:text-slate-300">{mode}</span>
          <nav aria-label="Main" className="flex flex-1 flex-wrap gap-1">
            {items.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === "/"}
                className={({ isActive }) =>
                  `rounded-md px-2.5 py-1.5 text-sm ${isActive ? "bg-indigo-50 font-semibold text-indigo-700 dark:bg-indigo-950 dark:text-indigo-200" : "text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"}`
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
          {state.status === "authenticated" && (
            <div className="flex items-center gap-3 text-sm">
              <span className="text-slate-600 dark:text-slate-300">{state.profile.displayName}</span>
              <button type="button" onClick={() => void logout()} className="text-indigo-600 hover:underline dark:text-indigo-300">
                Sign out
              </button>
            </div>
          )}
        </div>
      </header>
      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t border-slate-200 px-4 py-4 text-xs text-slate-500 dark:border-slate-800 dark:text-slate-400">
        <p className="mx-auto max-w-7xl">{status.data?.disclaimer ?? "Market data may be delayed, incomplete or unavailable. Nothing here is investment advice."}</p>
      </footer>
    </div>
  );
}
