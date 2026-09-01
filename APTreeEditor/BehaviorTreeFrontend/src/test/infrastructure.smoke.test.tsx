import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";

// Phase 0 smoke test: proves vitest + React Testing Library are wired up
// correctly (renders a component, resolves jest-dom matchers). App.tsx
// itself opens a live WebSocket on mount, so real component coverage per
// test-plan section 8 needs that mocked — that lands in a later phase.
function Greeting({ name }: { name: string }) {
  return <p>Hello, {name}!</p>;
}

describe("infrastructure smoke test", () => {
  it("renders a component and finds it via Testing Library", () => {
    render(<Greeting name="APTree" />);

    expect(screen.getByText("Hello, APTree!")).toBeInTheDocument();
  });
});
