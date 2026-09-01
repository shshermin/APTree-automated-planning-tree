import { test, expect } from "@playwright/test";

// Phase 0 placeholder: proves the Playwright harness is configured
// (starts the dev server, loads the page). The real editor->export
// flow test (test-plan #69) lands in a later phase.
test("editor page loads", async ({ page }) => {
  await page.goto("/");
  await expect(page.locator("body")).toBeVisible();
});
