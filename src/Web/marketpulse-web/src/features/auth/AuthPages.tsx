import { useMutation } from "@tanstack/react-query";
import { useEffect, useRef, useState, type FormEvent, type ReactNode } from "react";
import { Link, Navigate, useLocation, useNavigate, useSearchParams } from "react-router";
import { api } from "../../shared/api/client";
import type { AcceptedResponse } from "../../shared/api/types";
import { useAuth } from "../../shared/auth/AuthProvider";
import { DemoBanner } from "../../shared/ui/DemoBanner";
import { Button, Card, ErrorMessage, Notice, TextField, fieldErrors } from "../../shared/ui/primitives";

function AuthShell({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="min-h-screen">
      <DemoBanner />
      <div className="mx-auto max-w-md px-4 py-12">
        <h1 className="mb-6 text-center text-2xl font-bold">MarketPulse</h1>
        <Card title={title}>{children}</Card>
      </div>
    </div>
  );
}

export function LoginPage() {
  const { state, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const mutation = useMutation({ mutationFn: () => login(email, password) });
  const from = (location.state as { from?: string } | null)?.from ?? "/";

  if (state.status === "authenticated") return <Navigate to={from} replace />;

  const submit = (e: FormEvent) => {
    e.preventDefault();
    mutation.mutate(undefined, { onSuccess: () => navigate(from, { replace: true }) });
  };

  return (
    <AuthShell title="Sign in">
      <form onSubmit={submit} className="space-y-4">
        <TextField label="Email" name="email" type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        <TextField label="Password" name="password" type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        <ErrorMessage error={mutation.error} />
        <Button type="submit" className="w-full" disabled={mutation.isPending}>
          {mutation.isPending ? "Signing in…" : "Sign in"}
        </Button>
      </form>
      <div className="mt-4 flex justify-between text-sm">
        <Link to="/register" className="text-indigo-600 hover:underline dark:text-indigo-300">Create account</Link>
        <Link to="/forgot-password" className="text-indigo-600 hover:underline dark:text-indigo-300">Forgot password?</Link>
      </div>
    </AuthShell>
  );
}

export function RegisterPage() {
  const [form, setForm] = useState({ email: "", password: "", displayName: "" });
  const mutation = useMutation({ mutationFn: () => api<AcceptedResponse>("/auth/register", { method: "POST", body: form }) });

  return (
    <AuthShell title="Create your account">
      {mutation.isSuccess ? (
        <Notice>{mutation.data.message} Check your inbox for the verification link.</Notice>
      ) : (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            mutation.mutate();
          }}
          className="space-y-4"
        >
          <TextField label="Display name" name="displayName" required maxLength={100} value={form.displayName} onChange={(e) => setForm({ ...form, displayName: e.target.value })} error={fieldErrors(mutation.error, "displayName")} />
          <TextField label="Email" name="email" type="email" autoComplete="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} error={fieldErrors(mutation.error, "email")} />
          <TextField label="Password" name="password" type="password" autoComplete="new-password" required minLength={12} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} error={fieldErrors(mutation.error, "password")} />
          <p className="text-xs text-slate-500">At least 12 characters with upper-case, lower-case and a digit.</p>
          <ErrorMessage error={mutation.error && fieldErrors(mutation.error, "password") === undefined ? mutation.error : null} />
          <Button type="submit" className="w-full" disabled={mutation.isPending}>Create account</Button>
        </form>
      )}
      <p className="mt-4 text-sm">
        <Link to="/login" className="text-indigo-600 hover:underline dark:text-indigo-300">Back to sign in</Link>
      </p>
    </AuthShell>
  );
}

export function VerifyEmailPage() {
  const [params] = useSearchParams();
  const userId = params.get("userId");
  const token = params.get("token");
  const mutation = useMutation({ mutationFn: () => api<void>("/auth/verify-email", { method: "POST", body: { userId, token } }) });
  const started = useRef(false);

  useEffect(() => {
    if (!started.current && userId && token) {
      started.current = true;
      mutation.mutate();
    }
  }, [userId, token, mutation]);

  return (
    <AuthShell title="Verify email">
      {!userId || !token ? <Notice>This verification link is incomplete.</Notice> : null}
      {mutation.isPending && <p>Verifying…</p>}
      {mutation.isSuccess && <Notice>Your email is verified. You can now sign in.</Notice>}
      <ErrorMessage error={mutation.error} />
      <p className="mt-4 text-sm">
        <Link to="/login" className="text-indigo-600 hover:underline dark:text-indigo-300">Go to sign in</Link>
      </p>
    </AuthShell>
  );
}

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const mutation = useMutation({ mutationFn: () => api<AcceptedResponse>("/auth/forgot-password", { method: "POST", body: { email } }) });

  return (
    <AuthShell title="Reset your password">
      {mutation.isSuccess ? (
        <Notice>{mutation.data.message}</Notice>
      ) : (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            mutation.mutate();
          }}
          className="space-y-4"
        >
          <TextField label="Email" name="email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          <ErrorMessage error={mutation.error} />
          <Button type="submit" className="w-full" disabled={mutation.isPending}>Send reset link</Button>
        </form>
      )}
      <p className="mt-4 text-sm">
        <Link to="/login" className="text-indigo-600 hover:underline dark:text-indigo-300">Back to sign in</Link>
      </p>
    </AuthShell>
  );
}

export function ResetPasswordPage() {
  const [params] = useSearchParams();
  const [newPassword, setNewPassword] = useState("");
  const mutation = useMutation({
    mutationFn: () => api<void>("/auth/reset-password", { method: "POST", body: { userId: params.get("userId"), token: params.get("token"), newPassword } }),
  });

  return (
    <AuthShell title="Choose a new password">
      {mutation.isSuccess ? (
        <Notice>Your password has been changed and all sessions were signed out.</Notice>
      ) : (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            mutation.mutate();
          }}
          className="space-y-4"
        >
          <TextField label="New password" name="newPassword" type="password" autoComplete="new-password" required minLength={12} value={newPassword} onChange={(e) => setNewPassword(e.target.value)} error={fieldErrors(mutation.error, "newPassword")} />
          <ErrorMessage error={mutation.error && !fieldErrors(mutation.error, "newPassword") ? mutation.error : null} />
          <Button type="submit" className="w-full" disabled={mutation.isPending}>Change password</Button>
        </form>
      )}
      <p className="mt-4 text-sm">
        <Link to="/login" className="text-indigo-600 hover:underline dark:text-indigo-300">Go to sign in</Link>
      </p>
    </AuthShell>
  );
}

/** Route guard: renders children only for signed-in users. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { state } = useAuth();
  const location = useLocation();
  if (state.status === "loading") return <p className="p-8 text-center text-slate-500">Loading…</p>;
  if (state.status === "anonymous") return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  return <>{children}</>;
}
