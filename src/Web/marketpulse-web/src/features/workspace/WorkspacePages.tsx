import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../../shared/api/client";
import type { ExperienceMode, Preferences, ProfileResponse, Watchlist } from "../../shared/api/types";
import { useAuth } from "../../shared/auth/AuthProvider";
import { formatIst } from "../../shared/format";
import { usePreferences } from "../../shared/ui/Layout";
import { Button, Card, ErrorMessage, Notice, TextField, fieldErrors } from "../../shared/ui/primitives";

export function WatchlistsPage() {
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const list = useQuery({ queryKey: ["watchlists"], queryFn: () => api<Watchlist[]>("/watchlists") });
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["watchlists"] });

  const create = useMutation({
    mutationFn: () => api<Watchlist>("/watchlists", { method: "POST", body: { name } }),
    onSuccess: () => {
      setName("");
      void invalidate();
    },
  });
  const remove = useMutation({ mutationFn: (id: string) => api<void>(`/watchlists/${id}`, { method: "DELETE" }), onSuccess: () => void invalidate() });

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Watchlists</h1>
      <Notice>Instruments can be added to watchlists once an instrument master is imported (Phase 2).</Notice>
      <Card title="New watchlist">
        <form
          className="flex flex-wrap items-end gap-3"
          onSubmit={(e) => {
            e.preventDefault();
            create.mutate();
          }}
        >
          <div className="min-w-64 flex-1">
            <TextField label="Name" name="name" required maxLength={100} value={name} onChange={(e) => setName(e.target.value)} error={fieldErrors(create.error, "name")} />
          </div>
          <Button type="submit" disabled={create.isPending}>Create</Button>
        </form>
        <div className="mt-3">
          <ErrorMessage error={create.error && !fieldErrors(create.error, "name") ? create.error : null} />
        </div>
      </Card>
      <Card title="Your watchlists">
        <ErrorMessage error={list.error ?? remove.error} />
        {list.isLoading && <p className="text-sm text-slate-500">Loading…</p>}
        {list.data?.length === 0 && <p className="text-sm text-slate-500">No watchlists yet.</p>}
        <ul className="divide-y divide-slate-200 dark:divide-slate-800">
          {list.data?.map((w) => (
            <WatchlistRow key={w.id} watchlist={w} onDelete={() => remove.mutate(w.id)} onRenamed={() => void invalidate()} />
          ))}
        </ul>
      </Card>
    </div>
  );
}

function WatchlistRow({ watchlist, onDelete, onRenamed }: { watchlist: Watchlist; onDelete: () => void; onRenamed: () => void }) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(watchlist.name);
  const rename = useMutation({
    mutationFn: () => api<Watchlist>(`/watchlists/${watchlist.id}`, { method: "PUT", body: { name } }),
    onSuccess: () => {
      setEditing(false);
      onRenamed();
    },
  });

  return (
    <li className="flex flex-wrap items-center gap-3 py-3">
      {editing ? (
        <form
          className="flex flex-1 items-end gap-2"
          onSubmit={(e) => {
            e.preventDefault();
            rename.mutate();
          }}
        >
          <div className="flex-1">
            <TextField label="Rename" name={`rename-${watchlist.id}`} value={name} maxLength={100} onChange={(e) => setName(e.target.value)} />
          </div>
          <Button type="submit" disabled={rename.isPending}>Save</Button>
          <Button type="button" variant="secondary" onClick={() => setEditing(false)}>Cancel</Button>
        </form>
      ) : (
        <>
          <div className="flex-1">
            <p className="font-medium">{watchlist.name}</p>
            <p className="text-xs text-slate-500">Created {formatIst(watchlist.createdAtUtc)}</p>
          </div>
          <Button variant="secondary" onClick={() => setEditing(true)}>Rename</Button>
          <Button variant="danger" onClick={onDelete}>Delete</Button>
        </>
      )}
      {rename.error ? <div className="w-full"><ErrorMessage error={rename.error} /></div> : null}
    </li>
  );
}

export function SettingsPage() {
  const { state, setProfile } = useAuth();
  const queryClient = useQueryClient();
  const prefs = usePreferences();
  const profile = state.status === "authenticated" ? state.profile : null;
  const [displayName, setDisplayName] = useState(profile?.displayName ?? "");

  const saveProfile = useMutation({
    mutationFn: () => api<ProfileResponse>("/auth/profile", { method: "PUT", body: { displayName } }),
    onSuccess: setProfile,
  });
  const setMode = useMutation({
    mutationFn: (experienceMode: ExperienceMode) => api<Preferences>("/preferences", { method: "PUT", body: { experienceMode } }),
    onSuccess: (data) => queryClient.setQueryData(["preferences"], data),
  });
  const revokeAll = useMutation({ mutationFn: () => api<void>("/auth/sessions/revoke-all", { method: "POST" }) });

  if (!profile) return null;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Settings</h1>
      <Card title="Experience">
        <p className="mb-3 text-sm text-slate-600 dark:text-slate-300">Explore is guided and simplified. Pro shows every workspace with denser layouts.</p>
        <div className="flex gap-2" role="radiogroup" aria-label="Experience mode">
          {(["Explore", "Pro"] as const).map((m) => (
            <Button key={m} role="radio" aria-checked={prefs.data?.experienceMode === m} variant={prefs.data?.experienceMode === m ? "primary" : "secondary"} onClick={() => setMode.mutate(m)} disabled={setMode.isPending}>
              {m}
            </Button>
          ))}
        </div>
        <ErrorMessage error={setMode.error} />
      </Card>
      <Card title="Profile">
        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault();
            saveProfile.mutate();
          }}
        >
          <TextField label="Display name" name="displayName" required maxLength={100} value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
          <p className="text-sm text-slate-600 dark:text-slate-300">Email: {profile.email}</p>
          <p className="text-sm text-slate-600 dark:text-slate-300">Plan features: {profile.entitlements.map((e) => e.featureKey + (e.limit != null ? ` (max ${e.limit})` : "")).join(", ") || "none"}</p>
          <ErrorMessage error={saveProfile.error} />
          {saveProfile.isSuccess && <Notice>Saved.</Notice>}
          <Button type="submit" disabled={saveProfile.isPending}>Save profile</Button>
        </form>
      </Card>
      <Card title="Security">
        <p className="mb-3 text-sm text-slate-600 dark:text-slate-300">Sign out of every device. Already-open sessions stop working within 10 minutes.</p>
        <Button variant="danger" onClick={() => revokeAll.mutate()} disabled={revokeAll.isPending}>Sign out everywhere</Button>
        {revokeAll.isSuccess && <div className="mt-3"><Notice>All other sessions have been revoked.</Notice></div>}
        <ErrorMessage error={revokeAll.error} />
      </Card>
    </div>
  );
}
