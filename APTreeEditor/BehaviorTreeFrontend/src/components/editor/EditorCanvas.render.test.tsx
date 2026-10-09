import { describe, expect, it, vi, beforeAll } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import EditorCanvas from "./EditorCanvas";
import type { CanvasNode, NodeConnection } from "./types";
import { FLOW_NODES_KEY, DECORATOR_NODES_KEY } from "../sidebar/utils/constants";

// Renders through the real react-flow, not a mock. jsdom has no layout, so
// react-flow's measuring APIs are stubbed below.
beforeAll(() => {
  // Edges only render once both endpoint nodes have been measured, so the
  // stub has to actually fire the callback with a non-zero size.
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
  global.ResizeObserver = ResizeObserverStub as unknown as typeof ResizeObserver;

  class DOMMatrixReadOnlyStub {
    m22 = 1;
    constructor(transform?: string) {
      const scale = transform?.match(/scale\(([\d.]+)\)/);
      if (scale) this.m22 = parseFloat(scale[1]);
    }
  }
  // @ts-expect-error jsdom has no DOMMatrixReadOnly
  global.DOMMatrixReadOnly = DOMMatrixReadOnlyStub;

  // react-flow only computes handle bounds (needed for any edge path) when
  // offsetWidth/offsetHeight are non-zero, and reads the bounds themselves
  // from getBoundingClientRect(); both are always 0 in jsdom.
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

    // Measurement happens asynchronously.
    await waitFor(() => expect(container.querySelectorAll(".react-flow__edge")).toHaveLength(1));
  });

  it("renders no edges when there are no connections", () => {
    const { container } = render(
      <EditorCanvas nodes={[flowNode()]} connections={[]} onDropNode={vi.fn()} />
    );

    expect(container.querySelectorAll(".react-flow__edge")).toHaveLength(0);
  });

  it("skips a connection that references a node id not present in nodes, instead of crashing", async () => {
    const nodes: CanvasNode[] = [flowNode({ id: "a", name: "A" })];
    const connections: NodeConnection[] = [{ id: "c1", sourceNodeId: "a", targetNodeId: "does-not-exist" }];

    const { container } = render(
      <EditorCanvas nodes={nodes} connections={connections} onDropNode={vi.fn()} />
    );

    expect(screen.getByText("A")).toBeInTheDocument();
    // Let async measurement settle so the absence check is meaningful.
    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(container.querySelectorAll(".react-flow__edge")).toHaveLength(0);
  });

  it("renders an empty canvas without error when given no nodes", () => {
    const { container } = render(<EditorCanvas nodes={[]} onDropNode={vi.fn()} />);

    expect(container.querySelectorAll(".react-flow__node")).toHaveLength(0);
  });
});
