import { describe, expect, it } from "vitest";
import { createBehaviorNode } from "./flowNodeFactory";
import { DEFAULT_CANVAS_NODE_HEIGHT, DEFAULT_CANVAS_NODE_WIDTH } from "./types";
import {
  BT_NODES_KEY,
  DECORATOR_NODES_KEY,
  FLOW_NODES_KEY,
  SERVICE_NODES_KEY,
} from "../sidebar/utils/constants";
import type { BehaviorNodeOption } from "../sidebar/utils/types";
import { FLOW_SUCCESS_TYPES } from "../sidebar/utils/types";

// createBehaviorNode turns a sidebar catalog entry into the CanvasNode that EditorCanvas renders.

function option(overrides: Partial<BehaviorNodeOption> = {}): BehaviorNodeOption {
  return { id: "src-1", label: "PickUpHL", kind: "flow", typeLabel: "Flow", ...overrides } as BehaviorNodeOption;
}

describe("createBehaviorNode", () => {
  it("maps each sidebar kind to its canvas category", () => {
    expect(createBehaviorNode({ option: option({ kind: "flow" }) }).category).toBe(FLOW_NODES_KEY);
    expect(createBehaviorNode({ option: option({ kind: "decorator" }) }).category).toBe(DECORATOR_NODES_KEY);
    expect(createBehaviorNode({ option: option({ kind: "service" }) }).category).toBe(SERVICE_NODES_KEY);
    expect(createBehaviorNode({ option: option({ kind: "nodeGraph" }) }).category).toBe(BT_NODES_KEY);
  });

  it("copies the option's identity and label onto the node", () => {
    const node = createBehaviorNode({ option: option({ id: "abc", label: "MyFlow", typeLabel: "Custom" }) });

    expect(node.sourceId).toBe("abc");
    expect(node.name).toBe("MyFlow");
    expect(node.typeLabel).toBe("Custom");
    expect(node.kind).toBe("behaviorNode");
  });

  it("generates a fresh id for every call, even with the same option", () => {
    const opt = option();
    const a = createBehaviorNode({ option: opt });
    const b = createBehaviorNode({ option: opt });

    expect(a.id).not.toBe(b.id);
  });

  it("defaults to position (120, 120) and the standard node size", () => {
    const node = createBehaviorNode({ option: option() });

    expect(node.x).toBe(120);
    expect(node.y).toBe(120);
    expect(node.width).toBe(DEFAULT_CANVAS_NODE_WIDTH);
    expect(node.height).toBe(DEFAULT_CANVAS_NODE_HEIGHT);
  });

  it("places the node at the given drop position", () => {
    const node = createBehaviorNode({ option: option(), position: { x: 42, y: 7 } });

    expect(node.x).toBe(42);
    expect(node.y).toBe(7);
  });

  it("assigns a default success type only for flow nodes without an explicit one", () => {
    expect(createBehaviorNode({ option: option({ kind: "flow" }) }).successType).toBe(FLOW_SUCCESS_TYPES[0]);
    expect(createBehaviorNode({ option: option({ kind: "decorator" }) }).successType).toBeUndefined();
  });

  it("honors an explicit defaultSuccessType on a flow option", () => {
    const custom = FLOW_SUCCESS_TYPES[FLOW_SUCCESS_TYPES.length - 1];
    const node = createBehaviorNode({ option: option({ kind: "flow", defaultSuccessType: custom }) });

    expect(node.successType).toBe(custom);
  });
});
