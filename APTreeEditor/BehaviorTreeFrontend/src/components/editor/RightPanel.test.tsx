import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import RightPanel from "./RightPanel";
import type { RawSubtreeEntry, SubtreeLayout } from "../../utils/aptreeImport";
import type { CanvasNode } from "./types";

// The canvas only needs to show which layout it got.
vi.mock("./EditorCanvas", () => ({
  default: ({ nodes }: { nodes: CanvasNode[] }) => (
    <div data-testid="canvas">{nodes.map((n) => n.id).join(",")}</div>
  ),
}));

// Each layout's single node id is the entry's graph root, so the test can tell
// which import a displayed layout came from.
vi.mock("../../utils/aptreeImport", () => ({
  computeSubtreeLayout: vi.fn(
    (entry: RawSubtreeEntry): SubtreeLayout =>
      ({
        nodes: [{ id: entry.graph.rootId }],
        connections: [],
        discoveredActionTypes: [],
        discoveredActionInstances: [],
      }) as unknown as SubtreeLayout,
  ),
}));

function subtrees(rootId: string): Map<string, RawSubtreeEntry> {
  return new Map([["PickUpHL", { graph: { rootId, nodes: [], edges: [] }, index: 0 }]]);
}

function renderPanel(raw: Map<string, RawSubtreeEntry>) {
  return render(<RightPanel rawSubtreeGraphs={raw} nodes={[]} connections={[]} tickStatus={{}} />);
}

describe("RightPanel subtree layouts", () => {
  it("shows the selected subtree's layout", () => {
    renderPanel(subtrees("first-import"));

    fireEvent.click(screen.getByRole("button", { name: "PickUpHL" }));

    expect(screen.getByTestId("canvas").textContent).toBe("first-import");
  });

  /**
   * Regression: the layout cache was a ref cleared in an effect, i.e. after the
   * render, so a re-import kept showing the old layout of a same-named subtree.
   */
  it("shows the new layout after a re-import with the same subtree name", () => {
    const { rerender } = renderPanel(subtrees("first-import"));
    fireEvent.click(screen.getByRole("button", { name: "PickUpHL" }));

    rerender(
      <RightPanel rawSubtreeGraphs={subtrees("second-import")} nodes={[]} connections={[]} tickStatus={{}} />,
    );

    expect(screen.getByTestId("canvas").textContent).toBe("second-import");
  });

  it("opens on the live tab when there are no subtrees", () => {
    renderPanel(new Map());

    expect(screen.getByText("No active execution")).toBeVisible();
  });
});
