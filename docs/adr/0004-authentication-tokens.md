# ADR-0004: Authentication and session tokens

- Status: Accepted (implementation in Phase 1)
- Date: 2026-11-07
- Spec reference: §4.1, §13

## Decision
- ASP.NET Core Identity for user storage and password hashing. Endpoints are custom (`/api/v1/auth/*`).
- Short-lived JWT access token (≈10 min), returned in the response body and held **in memory only** by the SPA.
- Refresh token:
  - Opaque random value, stored **hashed** in `auth.RefreshTokens`.
  - Delivered as an `HttpOnly; Secure; SameSite=Strict` cookie scoped to `/api/v1/auth`.
  - Rotated on every use.
  - Reuse of an already-rotated token revokes the whole token family (theft detection).
- Email verification is required before access to data features. Dev email goes to Mailpit.
- Tenancy for MVP: one user = one tenant. All user-owned rows carry both `TenantId` and `UserId`.

## Consequences
- XSS cannot read the refresh token. CSRF on the refresh endpoint is mitigated by SameSite=Strict plus a custom header check.
