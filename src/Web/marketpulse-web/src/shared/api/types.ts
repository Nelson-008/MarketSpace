import type { components } from "./schema";

/** Types generated from docs/api/openapi.json (run `npm run generate:api` after API changes). */
type Schemas = components["schemas"];

export type ApiErrorBody = Schemas["ApiError"];
export type AuthResponse = Schemas["AuthResponse"];
export type ProfileResponse = Schemas["ProfileResponse"];
export type SystemStatus = Schemas["SystemStatusResponse"];
export type Watchlist = Schemas["WatchlistResponse"];
export type Preferences = Schemas["PreferencesResponse"];
export type ExperienceMode = Schemas["ExperienceMode"];
export type AcceptedResponse = Schemas["AcceptedResponse"];
