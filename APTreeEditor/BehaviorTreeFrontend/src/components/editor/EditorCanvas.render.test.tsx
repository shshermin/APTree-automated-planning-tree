import { describe, expect, it, vi, beforeAll } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import EditorCanvas from "./EditorCanvas";
import type { CanvasNode, NodeConnection } from "./types";
import { FLOW_NODES_KEY, DECORATOR_NODES_KEY } from "../sidebar/utils/constants";

// Nodes and edges the editor is given actually
// reach the DOM through the real EditorCanvas -> react-flow render path
// (not a shallow mock of react-flow).
//
// react-flow measures nodes via ResizeObserver, which jsdom doesn't
// implement; a minimal stub is enough for it to lay nodes out.
beforeAll(() => {
  // jsdom has no layout engine, so every element reports a 0x0 bounding box.
  // react-flow only renders an edge once both its endpoint nodes have been
  // "measured" via ResizeObserver, so the stub must actually invoke the
  // callback (with a plausible non-zero size) rather than sit idle.
  class ResizeObserverStub {
    #callback: ResizeObserverCallback;
    constructor(callback: ResizeObserverCallback) {
      this.#callback = callback;
    }
    observe(target: Element) {
      const rect = { width: 240, height: 180 };
      const entry = {
        target,
        contentRect: rect,
        borderBoxSize: [{ inlineSize: rect.width, blockSize: rect.height }],
        contentBoxSize: [{ inlineSize: rect.width, blockSize: rect.height }],
      } as unknown as ResizeObserverEntry;
      queueMicrotask(() => this.#callback([entry], this as unknown as ResizeObserver));
    }
    unobserve() {}
    disconnect() {}
  }
  // @ts-expect-error jsdom has no ResizeObserver
  global.ResizeObserver = ResizeObserverStub;

  // jsdom also has no DOMMatrixReadOnly, which react-flow's node-measuring
  // path constructs unconditionally once ResizeObserver fires.
  class DOMMatrixReadOnlyStub {
    m22 = 1;
    constructor(transform?: string) {
      const scale = transform?.match(/scale\(([\d.]+)\)/);
      if (scale) this.m22 = parseFloat(scale[1]);
    }
  }
  // @ts-expect-error jsdom has no DOMMatrixReadOnly
  global.DOMMatrixReadOnly = DOMMatrixReadOnlyStub;

  // jsdom has no layout engine: getBoundingClientRect() and offsetWidth/
  // offsetHeight are always 0. react-flow's node-dimension update
  // (updateNodeDimensions in @reactflow/core) reads offsetWidth/offsetHeight
  // via getDimensions() and only computes handle bounds - which an edge
  // needs to have any path at all - `if (dimensions.width && dimensions.
  // height)`. Handle bounds themselves come from each handle's
  // getBoundingClientRect() (getHandleBounds). Without both stubs no edge
  // is ever drawn, regardless of how "connected" the data model is.
  Element.prototype.getBoundingClientRect = () => ({
    x: 0, y: 0, width: 20, height: 20, top: 0, left: 0, right: 20, bottom: 20,
    toJSON() { return this; },
  });
  Object.defineProperty(HTMLElement.prototype, "offsetWidth", { configurable: true, get: () => 240 });
  Object.defineProperty(HTMLElement.prototype, "offsetHeight", { configurable: true, get: () => 180 });
});

function flowNode(overrides: Partial<CanvasNode> = {}): CanvasNode {
  return {
    id: "n1",
    sourceId: "src-flow",
    name: "Main",
    typeLabel: "FlowNode",
    category: FLOW_NODES_KEY,
    kind: "behaviorNode",
    x: 0,
    y: 0,
    width: 240,
    height: 180,
    ...overrides,
  };
}

describe("EditorCanvas", () => {
  it("renders a label for every node it is given", () => {
    const nodes: CanvasNode[] = [
      flowNode({ id: "n1", name: "Main" }),
      flowNode({ id: "n2", name: "Sub", category: DECORATOR_NODES_KEY }),
    ];

    render(<EditorCanvas nodes={nodes} onDropNode={vi.fn()} />);

    expect(screen.getByText("Main")).toBeInTheDocument();
    expect(screen.getByText("Sub")).toBeInTheDocument();
  });

  it("renders one edge (react-flow edge path) per connection", async () => {
    const nodes: CanvasNode[] = [flowNode({ id: "a", name: "A" }), flowNode({ id: "b", name: "B", x: 400 })];
    const connections: NodeConnection[] = [
      { id: "c1", sourceNodeId: "a", targetNodeId: "b", sourcePort: "right", targetPort: "left" },
    ];

    const { container } = render(
      <EditorCanvas nodes={nodes} connections={connections} onDropNode={vi.fn()} />
    );

    // react-flow only draws an edge once both endpoint nodes report their
    // measured size (via the ResizeObserver stub above), which happens async.
    await waitFor(() => expect(container.querySelectorAll(".react-flow__edge")).toHaveLength(1));
  });

  it("renders no edges when there are no connections", () => {
    const { container } = render(
      <EditorCanvas nodes={[flowNode()]} connections={[]} onDropNode={vi.fn()} />
    );

    expect(container.querySelectorAll(".react-flow__edge")).toHaveLength(0);
  });

  it("skips a connection that references a node id not present in nodes, instead of crashing", async () => {
    // Finding: dangling connections (e.g. after a node was deleted but the
    // connection list wasn't pruned) don't throw - react-flow silently
    // drops any edge whose source/target id has no matching node.
    const nodes: CanvasNode[] = [flowNode({ id: "a", name: "A" })];
    const connections: NodeConnection[] = [{ id: "c1", sourceNodeId: "a", targetNodeId: "does-not-exist" }];

    const { container } = render(
      <EditorCanvas nodes={nodes} connections={connections} onDropNode={vi.fn()} />
    );

    expect(screen.getByText("A")).toBeInTheDocument();
    // Give react-flow's async measurement a chance to run before asserting
    // absence, so this isn't just passing because nothing settled yet.
    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(container.querySelectorAll(".react-flow__edge")).toHaveLength(0);
  });

  it("renders an empty canvas without error when given no nodes", () => {
    const { container } = render(<EditorCanvas nodes={[]} onDropNode={vi.fn()} />);

    expect(container.querySelectorAll(".react-flow__node")).toHaveLength(0);
  });
});
