/** All instants arrive as UTC ISO-8601; Indian market users see Asia/Kolkata (spec §7). */
export const INDIA_TIME_ZONE = "Asia/Kolkata";

const dateTime = new Intl.DateTimeFormat("en-IN", {
  timeZone: INDIA_TIME_ZONE,
  year: "numeric",
  month: "short",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hour12: false,
});

/** Formats a UTC instant for display in IST, e.g. "07 Nov 2026, 09:15:00 IST". */
export function formatIst(utcIso: string | null | undefined): string {
  if (!utcIso) return "—";
  const date = new Date(utcIso);
  if (Number.isNaN(date.getTime())) return "—";
  return `${dateTime.format(date)} IST`;
}
