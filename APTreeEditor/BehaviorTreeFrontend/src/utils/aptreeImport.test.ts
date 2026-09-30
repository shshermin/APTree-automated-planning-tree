import { describe, expect, it } from "vitest";
import {
  aptreeGraphToCanvasGraph,
  aptreeGraphsToCanvasGraph,
  computeSubtreeLayout,
  normalizeAptreeValidateResponse,
  type AptreeGraph,
} from "./aptreeImport";
import type { ActionType } from "../components/sidebar/utils/types";
import {
  ACTION_INSTANCES_KEY,
  BT_NODES_KEY,
  DECORATOR_NODES_KEY,
  FLOW_NODES_KEY,
  SERVICE_NODES_KEY,
} from "../components/sidebar/utils/constants";

// The literal wording ("a tree created in the editor
// exports JSON matching the schema the execution engine expects") describes a
// feature that does not exist: grepping App.tsx's handleExportCanvasGraph
// shows the editor's only "Export Graph (JSON)" action produces an internal
// ExportedCanvasGraphV2 round-trip format, never fed to the execution engine
// or validated against the DSL/BehaviorTreeModel.json schema. The only real
// schema-crossing logic runs in the opposite direction: the backend's
// /api/aptree/validate response (an AptreeGraph) being converted into the
// editor's CanvasGraph. These tests cover that conversion instead.

describe("normalizeAptreeValidateResponse", () => {
  it("reports an error and does not throw for a non-object payload", () => {
    expect(normalizeAptreeValidateResponse(null)).toEqual({
      ok: false,
      errors: ["Invalid response from backend"],
    });
    expect(normalizeAptreeValidateResponse("garbage")).toEqual({
      ok: false,
      errors: ["Invalid response from backend"],
    });
    expect(normalizeAptreeValidateResponse(undefined)).toEqual({
      ok: false,
      errors: ["Invalid response from backend"],
    });
  });

  it("passes through scalar fields and normalizes a single embedded graph", () => {
    const result = normalizeAptreeValidateResponse({
      ok: true,
      treeName: "Main",
      errors: ["e1"],
      findings: ["f1"],
      graph: { rootId: "n1", nodes: [{ id: "n1", kind: "flow", label: "Root" }], edges: [] },
    });

    expect(result.ok).toBe(true);
    expect(result.treeName).toBe("Main");
    expect(result.errors).toEqual(["e1"]);
    expect(result.findings).toEqual(["f1"]);
    expect(result.graph?.nodes).toHaveLength(1);
    expect(result.graph?.rootId).toBe("n1");
  });

  it("drops fields with the wrong type instead of passing them through unchecked", () => {
    const result = normalizeAptreeValidateResponse({
      ok: "yes", // wrong type, not boolean
      treeName: 123, // wrong type, not string
      errors: "not-an-array",
    });

    expect(result.ok).toBeUndefined();
    expect(result.treeName).toBeUndefined();
    expect(result.errors).toBeUndefined();
  });

  it("normalizes a multi-graph .bt file payload (graphs array)", () => {
    const result = normalizeAptreeValidateResponse({
      graphs: [
        { name: "main", rootId: "a", nodes: [{ id: "a", kind: "flow", label: "A" }], edges: [] },
        { name: "sub1", rootId: "b", nodes: [{ id: "b", kind: "flow", label: "B" }], edges: [] },
      ],
    });

    expect(result.graphs).toHaveLength(2);
    expect(result.graphs?.[0]?.name).toBe("main");
    expect(result.graphs?.[1]?.name).toBe("sub1");
  });

  it("coerces missing/non-string node and edge fields to safe defaults instead of 'undefined' strings", () => {
    // Finding-relevant: normalizeGraph uses String(node.id ?? "") not
    // String(node.id), so a missing id becomes "" rather than the literal
    // string "undefined" that a naive String(x) coercion would produce.
    const result = normalizeAptreeValidateResponse({
      graph: {
        rootId: null,
        nodes: [{}],
        edges: [{}],
      },
    });

    expect(result.graph?.nodes[0]).toMatchObject({ id: "", kind: "", label: "" });
    expect(result.graph?.edges[0]).toMatchObject({ id: "", sourceId: "", targetId: "", kind: "" });
  });

  it("filters non-object entries out of nodes/edges/graphs arrays instead of crashing", () => {
    const result = normalizeAptreeValidateResponse({
      graph: {
        rootId: "a",
        nodes: [{ id: "a", kind: "flow", label: "A" }, null, "garbage", 42],
        edges: [{ id: "e1", sourceId: "a", targetId: "a", kind: "contains" }, null],
      },
    });

    expect(result.graph?.nodes).toHaveLength(1);
    expect(result.graph?.edges).toHaveLength(1);
  });
});

describe("aptreeGraphToCanvasGraph — root resolution", () => {
  const flowNode = (id: string) => ({ id, kind: "flow", label: "Flow" });

  it("uses the explicit rootId when it references a real node", () => {
    const graph: AptreeGraph = { rootId: "n2", nodes: [flowNode("n1"), flowNode("n2")], edges: [] };
    const result = aptreeGraphToCanvasGraph(graph, []);
    const rootCanvasNode = result.graph.nodes.find((n) => n.id === result.graph.rootNodeId);
    expect(rootCanvasNode?.sourceId ?? rootCanvasNode?.id).toContain("n2");
  });

  // Finding: aptreeGraphToCanvasGraph internally computes a fallback root
  // (first flow-kind node, else the first node) for LAYOUT purposes only
  // (positioning, the rootIsFlow branch, etc.) - see the `rootId` local at
  // the top of the function. But the `rootNodeId` actually returned to the
  // caller (used at line ~1345) is derived straight from `graph.rootId` and
  // ignores that fallback entirely. So whenever the backend sends a
  // missing/invalid rootId, the editor still lays the tree out sensibly but
  // reports no root node at all - e.g. anything that treats rootNodeId as
  // "the entry point to run/highlight" silently has nothing to point at.
  it("BUG: does not surface the internal fallback root - returns null rootNodeId when rootId is missing, even though a flow node exists", () => {
    const graph: AptreeGraph = {
      rootId: null,
      nodes: [{ id: "a", kind: "action", label: "Act" }, flowNode("b")],
      edges: [],
    };
    const result = aptreeGraphToCanvasGraph(graph, []);
    expect(result.graph.rootNodeId).toBeNull();
    // The node itself is still rendered and positioned by the fallback path.
    expect(result.graph.nodes.some((n) => n.id === "bt-import-b")).toBe(true);
  });

  it("BUG: returns null rootNodeId when rootId is missing and no flow node exists either", () => {
    const graph: AptreeGraph = {
      rootId: null,
      nodes: [{ id: "a", kind: "action", label: "Act" }, { id: "b", kind: "action", label: "Act2" }],
      edges: [],
    };
    const result = aptreeGraphToCanvasGraph(graph, []);
    expect(result.graph.rootNodeId).toBeNull();
  });

  it("BUG: returns null rootNodeId when rootId references a node id that doesn't exist, instead of falling back", () => {
    const graph: AptreeGraph = { rootId: "does-not-exist", nodes: [flowNode("b")], edges: [] };
    const result = aptreeGraphToCanvasGraph(graph, []);
    expect(result.graph.rootNodeId).toBeNull();
  });
});

describe("aptreeGraphToCanvasGraph — node kind mapping", () => {
  it("maps each node kind to its canvas category and kind", () => {
    const graph: AptreeGraph = {
      rootId: "flow1",
      nodes: [
        { id: "flow1", kind: "flow", label: "Flow", name: "MyFlow" },
        { id: "svc1", kind: "service", label: "MyService" },
        { id: "dec1", kind: "decorator", label: "MyDecorator", name: "Inverter" },
        { id: "bt1", kind: "btNode", label: "MyNode", name: "Node" },
      ],
      edges: [],
    };

    const result = aptreeGraphToCanvasGraph(graph, []);
    const byPrefix = (id: string) => result.graph.nodes.find((n) => n.id === "bt-import-" + id)!;

    expect(byPrefix("flow1")).toMatchObject({ category: FLOW_NODES_KEY, kind: "behaviorNode" });
    expect(byPrefix("svc1")).toMatchObject({ category: SERVICE_NODES_KEY, kind: "behaviorNode" });
    expect(byPrefix("dec1")).toMatchObject({ category: DECORATOR_NODES_KEY, kind: "behaviorNode" });
    expect(byPrefix("bt1")).toMatchObject({ category: BT_NODES_KEY, kind: "behaviorNode" });
  });

  it("maps an action node to an actionInstance canvas node under ACTION_INSTANCES_KEY", () => {
    const graph: AptreeGraph = {
      rootId: "flow1",
      nodes: [
        { id: "flow1", kind: "flow", label: "Flow" },
        { id: "act1", kind: "action", label: "PickUpHL", astType: "ASTPickUpHL" },
      ],
      edges: [],
    };

    const result = aptreeGraphToCanvasGraph(graph, []);
    const actionNode = result.graph.nodes.find((n) => n.id === "bt-import-act1")!;

    expect(actionNode.category).toBe(ACTION_INSTANCES_KEY);
    expect(actionNode.kind).toBe("actionInstance");
    expect(actionNode.typeLabel).toBe("PickUpHL");
  });

  it("resolves a known action type by name (case-insensitively, ignoring the AST prefix)", () => {
    const knownType: ActionType = {
      id: "pickup-type",
      name: "PickUpHL",
      type: "GenericBTAction",
      properties: [{ id: "loc-prop", name: "loc", valueType: "string" }],
    };
    const graph: AptreeGraph = {
      rootId: "act1",
      nodes: [
        {
          id: "act1",
          kind: "action",
          label: "pickuphl (table)",
          astType: "ASTPickUpHL",
          paramNames: ["loc"],
          paramValues: ["table"],
        },
      ],
      edges: [],
    };

    const result = aptreeGraphToCanvasGraph(graph, [knownType]);

    // A resolved action type is never re-discovered.
    expect(result.discoveredActionTypes).toHaveLength(0);
    const actionNode = result.graph.nodes.find((n) => n.id === "bt-import-act1")!;
    expect(actionNode.typeId).toBe("pickup-type");
    expect(result.discoveredActionInstances[0]?.propertyValues).toMatchObject({ "loc-prop": "table" });
  });

  it("discovers a new generic action type (with positional param-N properties) when the type isn't in the catalog", () => {
    const graph: AptreeGraph = {
      rootId: "act1",
      nodes: [{ id: "act1", kind: "action", label: "MoveTo (x y)", astType: "ASTMoveTo" }],
      edges: [],
    };

    const result = aptreeGraphToCanvasGraph(graph, []);

    expect(result.discoveredActionTypes).toHaveLength(1);
    const discovered = result.discoveredActionTypes[0]!;
    expect(discovered.name).toBe("MoveTo");
    // paramNames/paramValues weren't given on the node, so args are parsed
    // out of the "(x y)" portion of the label as a fallback.
    expect(discovered.properties.map((p) => p.name)).toEqual(["param1", "param2"]);
    expect(result.discoveredActionInstances[0]?.propertyValues).toMatchObject({
      "param-1": "x",
      "param-2": "y",
    });
  });

  it("returns no canvas node at all for an unrecognized node kind, instead of a placeholder or a crash", () => {
    // Finding: any node kind outside action/service/decorator/flow/btNode is
    // silently dropped from the canvas (canvasNode stays null and is never
    // pushed) - it neither renders nor produces an error.
    const graph: AptreeGraph = {
      rootId: null,
      nodes: [{ id: "mystery1", kind: "somethingNew", label: "???" }],
      edges: [],
    };

    const result = aptreeGraphToCanvasGraph(graph, []);
    expect(result.graph.nodes).toHaveLength(0);
  });

  it("computes hasOutgoing from real outgoing edges but ignores 'member' edges", () => {
    const graph: AptreeGraph = {
      rootId: "flow1",
      nodes: [
        { id: "flow1", kind: "flow", label: "Flow" },
        { id: "flow2", kind: "flow", label: "Flow2" },
        { id: "graphOnly", kind: "flow", label: "OnlyMember" },
      ],
      edges: [
        { id: "e1", sourceId: "flow1", targetId: "flow2", kind: "contains" },
        { id: "e2", sourceId: "graphOnly", targetId: "flow2", kind: "member" },
      ],
    };

    const result = aptreeGraphToCanvasGraph(graph, []);
    const flow1 = result.graph.nodes.find((n) => n.id === "bt-import-flow1")!;
    const graphOnly = result.graph.nodes.find((n) => n.id === "bt-import-graphOnly")!;

    expect(flow1.hasOutgoing).toBe(true);
    expect(graphOnly.hasOutgoing).toBe(false);
  });
});

describe("aptreeGraphsToCanvasGraph — multi-graph .bt files", () => {
  const flow = (id: string, name?: string) => ({ id, kind: "flow", label: "Flow", name });

  it("prefixes the main graph's node/edge/connection ids so they don't collide with subtree ids", () => {
    const main: AptreeGraph = {
      rootId: "a",
      nodes: [flow("a"), flow("b")],
      edges: [{ id: "e1", sourceId: "a", targetId: "b", kind: "contains" }],
    };

    const result = aptreeGraphsToCanvasGraph([main], []);

    expect(result.graph.nodes.every((n) => n.id.startsWith("bt-tree-0-"))).toBe(true);
    expect(result.graph.rootNodeId).toBe("bt-tree-0-bt-import-a");
  });

  it("stores named subtree graphs (index 1..N) raw, without laying them out eagerly", () => {
    const main: AptreeGraph = { rootId: "a", nodes: [flow("a")], edges: [] };
    const subtree: AptreeGraph = { name: "PickAndPlace", rootId: "s1", nodes: [flow("s1")], edges: [] };

    const result = aptreeGraphsToCanvasGraph([main, subtree], []);

    expect(result.rawSubtreeGraphs.has("PickAndPlace")).toBe(true);
    expect(result.rawSubtreeGraphs.get("PickAndPlace")?.index).toBe(1);
  });

  it("silently drops a subtree graph that has no name, instead of storing it under a generated key", () => {
    // Finding: rawSubtreeGraphs.set() is only called `if (subtreeGraph.name)`,
    // so an unnamed subtree graph in the .bt file is neither placed on the
    // canvas nor made reachable via the subtree panel - it disappears.
    const main: AptreeGraph = { rootId: "a", nodes: [flow("a")], edges: [] };
    const unnamedSubtree: AptreeGraph = { rootId: "s1", nodes: [flow("s1")], edges: [] };

    const result = aptreeGraphsToCanvasGraph([main, unnamedSubtree], []);

    expect(result.rawSubtreeGraphs.size).toBe(0);
  });

  it("only surfaces discoveredActionTypes/-Instances from the main graph, not from unlaid-out subtrees", () => {
    // Finding: subtree action types/instances are only discovered once
    // computeSubtreeLayout() runs for that subtree (e.g. when the user opens
    // it), so the result of aptreeGraphsToCanvasGraph never includes them.
    const main: AptreeGraph = {
      rootId: "act1",
      nodes: [{ id: "act1", kind: "action", label: "MainAction" }],
      edges: [],
    };
    const subtree: AptreeGraph = {
      name: "Sub",
      rootId: "act2",
      nodes: [{ id: "act2", kind: "action", label: "SubAction" }],
      edges: [],
    };

    const result = aptreeGraphsToCanvasGraph([main, subtree], []);

    expect(result.discoveredActionTypes.map((t) => t.name)).toEqual(["MainAction"]);
  });

  it("returns an empty result for an empty graphs array instead of throwing", () => {
    const result = aptreeGraphsToCanvasGraph([], []);
    expect(result.graph.nodes).toHaveLength(0);
    expect(result.graph.rootNodeId).toBeNull();
  });
});

describe("computeSubtreeLayout", () => {
  it("lays out a raw subtree entry on demand, prefixed by its own graph index", () => {
    const subtreeGraph: AptreeGraph = {
      name: "PickAndPlace",
      rootId: "s1",
      nodes: [{ id: "s1", kind: "btNode", label: "Root" }, { id: "s2", kind: "action", label: "Action" }],
      edges: [{ id: "e1", sourceId: "s1", targetId: "s2", kind: "child" }],
    };

    const layout = computeSubtreeLayout({ graph: subtreeGraph, index: 3 }, []);

    expect(layout.nodes.every((n) => n.id.startsWith("bt-tree-3-"))).toBe(true);
    expect(layout.rootNodeId).toBe("bt-tree-3-bt-import-s1");
    expect(layout.connections[0]?.sourceNodeId).toBe("bt-tree-3-bt-import-s1");
    expect(layout.connections[0]?.targetNodeId).toBe("bt-tree-3-bt-import-s2");
  });

  it("discovers action types/instances scoped to that single subtree", () => {
    const subtreeGraph: AptreeGraph = {
      name: "Sub",
      rootId: "act1",
      nodes: [{ id: "act1", kind: "action", label: "SubOnlyAction" }],
      edges: [],
    };

    const layout = computeSubtreeLayout({ graph: subtreeGraph, index: 1 }, []);

    expect(layout.discoveredActionTypes.map((t) => t.name)).toEqual(["SubOnlyAction"]);
  });
});
