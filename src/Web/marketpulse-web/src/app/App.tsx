import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Route, Routes } from "react-router";
import { ApiError } from "../shared/api/client";
import { AuthProvider } from "../shared/auth/AuthProvider";
import { Layout } from "../shared/ui/Layout";
import { ForgotPasswordPage, LoginPage, RegisterPage, RequireAuth, ResetPasswordPage, VerifyEmailPage } from "../features/auth/AuthPages";
import { HomePage, PlannedPage } from "../features/home/HomePages";
import { SettingsPage, WatchlistsPage } from "../features/workspace/WorkspacePages";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Do not retry client errors (4xx); they will not succeed on retry.
      retry: (count, error) => !(error instanceof ApiError && error.status < 500) && count < 2,
    },
  },
});

const planned: { path: string; title: string; phase: string; description: string }[] = [
  { path: "/discover", title: "Discover Stocks", phase: "Phase 5", description: "Fundamental filters, sector discovery, saved screens and guided explanations." },
  { path: "/institutional", title: "Institutional Intelligence", phase: "Phase 4", description: "Aggregate FII/DII flows, bulk/block deals and quarterly shareholding, each kept as a separate, source-linked dataset." },
  { path: "/scanner", title: "Market Scanner", phase: "Phase 3", description: "Unusual volume, turnover, VWAP, breakouts and custom filters, each result showing the exact conditions matched." },
  { path: "/research", title: "Stock Research", phase: "Phase 5", description: "Company overview, financials, ratios with formulas, valuation context, ownership and disclosures." },
  { path: "/ai-analyst", title: "AI Research Analyst", phase: "Phase 7", description: "Evidence-backed, cited reports that separate verified facts, calculations, interpretation and risks." },
  { path: "/terminal", title: "Trading Terminal", phase: "Phase 3", description: "Charts, indicators, alerts and intraday/swing scanners with freshness labels." },
  { path: "/fno", title: "F&O Intelligence", phase: "Phase 10", description: "Option chain, OI, IV and Greeks, only with licensed data and recorded model inputs." },
  { path: "/strategy-lab", title: "Strategy Lab", phase: "Phase 9", description: "Backtests with costs, slippage and look-ahead controls." },
  { path: "/paper-trading", title: "Paper Trading", phase: "Phase 8", description: "Virtual portfolios and simulated orders. Never routed to a real broker." },
];

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/verify-email" element={<VerifyEmailPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            <Route
              element={
                <RequireAuth>
                  <Layout />
                </RequireAuth>
              }
            >
              <Route index element={<HomePage />} />
              <Route path="/watchlists" element={<WatchlistsPage />} />
              <Route path="/settings" element={<SettingsPage />} />
              {planned.map((p) => (
                <Route key={p.path} path={p.path} element={<PlannedPage title={p.title} phase={p.phase} description={p.description} />} />
              ))}
              <Route path="*" element={<PlannedPage title="Not found" phase="—" description="This page does not exist." />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}
