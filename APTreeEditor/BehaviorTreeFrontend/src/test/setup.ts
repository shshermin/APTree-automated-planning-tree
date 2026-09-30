import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";

// Without this, React Testing Library leaves each test's render() mounted in
// the shared jsdom document, so anything rendered by an earlier test in the
// same file is still there (and matched by getByText etc.) in later tests.
afterEach(() => {
  cleanup();
});
