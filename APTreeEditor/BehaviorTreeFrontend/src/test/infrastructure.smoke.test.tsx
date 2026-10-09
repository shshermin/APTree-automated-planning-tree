import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";

// Checks vitest + React Testing Library + jest-dom matchers are wired up.
function Greeting({ name }: { name: string }) {
  return <p>Hello, {name}!</p>;
}

describe("infrastructure smoke test", () => {
  it("renders a component and finds it via Testing Library", () => {
    render(<Greeting name="APTree" />);

    expect(screen.getByText("Hello, APTree!")).toBeInTheDocument();
  });
});
