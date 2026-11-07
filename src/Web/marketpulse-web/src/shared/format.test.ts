import { describe, expect, it } from "vitest";
import { formatIst } from "./format";

describe("formatIst", () => {
  it("renders a UTC instant in Asia/Kolkata (+05:30)", () => {
    expect(formatIst("2026-11-06T03:45:00Z")).toMatch(/06 Nov 2026.*09:15:00 IST/);
  });

  it("rolls the date over after 18:30 UTC", () => {
    expect(formatIst("2026-11-06T20:00:00Z")).toMatch(/07 Nov 2026.*01:30:00 IST/);
  });

  it("shows a dash for missing or invalid input", () => {
    expect(formatIst(null)).toBe("—");
    expect(formatIst("not a date")).toBe("—");
  });
});
