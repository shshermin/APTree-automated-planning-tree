import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";

// Unmount between tests so earlier renders don't leak into later queries.
afterEach(() => {
  cleanup();
});
