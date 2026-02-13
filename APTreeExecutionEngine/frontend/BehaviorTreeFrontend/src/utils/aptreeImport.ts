import type { CanvasNode, NodeConnection } from "../components/editor/types";
import {
  DEFAULT_CANVAS_NODE_HEIGHT,
  DEFAULT_CANVAS_NODE_WIDTH,
} from "../components/editor/types";
import {
  ACTION_TYPES_KEY,
  BT_NODES_KEY,
  DECORATOR_NODES_KEY,
  FLOW_NODES_KEY,
  SERVICE_NODES_KEY,
} from "../components/sidebar/utils/constants";
import {
  FLOW_SUCCESS_TYPES,
  type ActionType,
  type FlowSuccessType,
} from "../components/sidebar/utils/types";

export type AptreeValidateResponse = {
  ok?: boolean;
  treeName?: string;
  errors?: string[];
  findings?: string[];
  graph?: AptreeGraph;
};

export type AptreeGraph = {
  rootId: string | null;
  nodes: AptreeGraphNode[];
  edges: AptreeGraphEdge[];
};

export type AptreeGraphNode = {
  id: string;
  kind: string;
  label: string;
  name?: string;
  astType?: string;
  line?: number;
  successType?: string;
};

export type AptreeGraphEdge = {
  id: string;
  sourceId: string;
  targetId: string;
  kind: string;
  label?: string;
};

function isObject(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === "object";
}

export function normalizeAptreeValidateResponse(
  payload: unknown
): AptreeValidateResponse {
  if (!isObject(payload)) {
    return { ok: false, errors: ["Invalid response from backend"] };
  }

  const graph = isObject(payload.graph)
    ? normalizeGraph(payload.graph)
    : undefined;

  return {
    ok: typeof payload.ok === "boolean" ? payload.ok : undefined,
    treeName: typeof payload.treeName === "string" ? payload.treeName : undefined,
    errors: Array.isArray(payload.errors) ? (payload.errors as string[]) : undefined,
    findings: Array.isArray(payload.findings)
      ? (payload.findings as string[])
      : undefined,
    graph,
  };
}

function normalizeGraph(value: Record<string, unknown>): AptreeGraph | undefined {
  const rootId =
    typeof value.rootId === "string" ? value.rootId : value.rootId === null ? null : null;

  const nodes = Array.isArray(value.nodes)
    ? (value.nodes.filter(isObject).map((node) => ({
        id: String(node.id ?? ""),
        kind: String(node.kind ?? ""),
        label: String(node.label ?? ""),
        name: typeof node.name === "string" ? node.name : undefined,
        astType: typeof node.astType === "string" ? node.astType : undefined,
        line: typeof node.line === "number" ? node.line : undefined,
        successType:
          typeof node.successType === "string" ? node.successType : undefined,
      })) as AptreeGraphNode[])
    : [];

  const edges = Array.isArray(value.edges)
    ? (value.edges.filter(isObject).map((edge) => ({
        id: String(edge.id ?? ""),
        sourceId: String(edge.sourceId ?? ""),
        targetId: String(edge.targetId ?? ""),
        kind: String(edge.kind ?? ""),
        label: typeof edge.label === "string" ? edge.label : undefined,
      })) as AptreeGraphEdge[])
    : [];

  if (!nodes.length) {
    return { rootId, nodes: [], edges: [] };
  }

  return { rootId, nodes, edges };
}

type CanvasGraph = {
  nodes: CanvasNode[];
  connections: NodeConnection[];
  rootNodeId: string | null;
};

export function aptreeGraphToCanvasGraph(
  graph: AptreeGraph,
  actionTypes: ActionType[]
): CanvasGraph {
  const nodeById = new Map(graph.nodes.map((n) => [n.id, n] as const));

  const outgoing = new Map<string, string[]>();
  for (const edge of graph.edges) {
    if (!edge.sourceId || !edge.targetId) continue;
    if (!outgoing.has(edge.sourceId)) outgoing.set(edge.sourceId, []);
    outgoing.get(edge.sourceId)!.push(edge.targetId);
  }

  const rootId =
    graph.rootId && nodeById.has(graph.rootId)
      ? graph.rootId
      : graph.nodes[0]?.id ?? null;

  // BFS layering for a readable top-down tree layout.
  const levelByNode = new Map<string, number>();
  const orderInLevel = new Map<string, number>();
  const queue: string[] = [];

  if (rootId) {
    levelByNode.set(rootId, 0);
    queue.push(rootId);
  }

  while (queue.length) {
    const current = queue.shift()!;
    const currentLevel = levelByNode.get(current) ?? 0;
    const targets = outgoing.get(current) ?? [];

    for (const target of targets) {
      if (!nodeById.has(target)) continue;
      if (levelByNode.has(target)) continue;
      levelByNode.set(target, currentLevel + 1);
      queue.push(target);
    }
  }

  // place unreachable nodes after the main graph
  let maxLevel = 0;
  for (const lvl of levelByNode.values()) maxLevel = Math.max(maxLevel, lvl);
  for (const node of graph.nodes) {
    if (!levelByNode.has(node.id)) {
      levelByNode.set(node.id, maxLevel + 1);
      maxLevel = maxLevel + 1;
    }
  }

  // stable ordering: by level then by original list order
  const nodesByLevel = new Map<number, string[]>();
  for (const n of graph.nodes) {
    const lvl = levelByNode.get(n.id) ?? 0;
    if (!nodesByLevel.has(lvl)) nodesByLevel.set(lvl, []);
    nodesByLevel.get(lvl)!.push(n.id);
  }
  for (const ids of nodesByLevel.values()) {
    ids.forEach((id, index) => orderInLevel.set(id, index));
  }

  const actionTypeNameMap = new Map(
    actionTypes.map((t) => [t.name.trim().toLowerCase(), t] as const)
  );

  const isValidFlowSuccessType = (value: string | undefined): value is FlowSuccessType =>
    !!value && (FLOW_SUCCESS_TYPES as readonly string[]).includes(value);

  const canvasNodes: CanvasNode[] = graph.nodes.map((n) => {
    const lvl = levelByNode.get(n.id) ?? 0;
    const idx = orderInLevel.get(n.id) ?? 0;

    const spacingX = 320;
    const spacingY = 240;

    const position = {
      x: 120 + idx * spacingX,
      y: 80 + lvl * spacingY,
    };

    const hasOutgoing = (outgoing.get(n.id) ?? []).length > 0;

    const normalizedAst = n.astType?.replace(/^AST/, "") ?? "";

    if (n.kind === "action") {
      const typeName = normalizedAst || n.label.split(" ")[0] || "Action";
      const resolved = actionTypeNameMap.get(typeName.trim().toLowerCase());
      const typeId = resolved?.id ?? typeName;

      return {
        id: `bt-import-${n.id}`,
        sourceId: typeId,
        name: n.name ?? n.label,
        typeLabel: typeName,
        category: ACTION_TYPES_KEY,
        kind: "actionType",
        x: position.x,
        y: position.y,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        hasOutgoing,
      };
    }

    if (n.kind === "service") {
      return {
        id: `bt-import-${n.id}`,
        sourceId: n.name ?? n.id,
        name: n.name ?? n.label,
        typeLabel: "Service",
        category: SERVICE_NODES_KEY,
        kind: "behaviorNode",
        x: position.x,
        y: position.y,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        hasOutgoing,
      };
    }

    if (n.kind === "decorator") {
      return {
        id: `bt-import-${n.id}`,
        sourceId: n.name ?? n.id,
        name: n.name ?? n.label,
        typeLabel: "Decorator",
        category: DECORATOR_NODES_KEY,
        kind: "behaviorNode",
        x: position.x,
        y: position.y,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        hasOutgoing,
      };
    }

    if (n.kind === "flow") {
      const successType = isValidFlowSuccessType(n.successType)
        ? n.successType
        : undefined;

      return {
        id: `bt-import-${n.id}`,
        sourceId: n.name ?? n.id,
        name: n.name ?? n.label,
        typeLabel: "Flow",
        category: FLOW_NODES_KEY,
        kind: "behaviorNode",
        x: position.x,
        y: position.y,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        successType,
        hasOutgoing,
      };
    }

    if (n.kind === "nodeGraph") {
      return {
        id: `bt-import-${n.id}`,
        sourceId: n.name ?? n.id,
        name: n.name ?? n.label,
        typeLabel: "NodeGraph",
        category: BT_NODES_KEY,
        kind: "behaviorNode",
        x: position.x,
        y: position.y,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        hasOutgoing,
      };
    }

    // default: btNode / unknown
    return {
      id: `bt-import-${n.id}`,
      sourceId: n.name ?? n.id,
      name: n.name ?? n.label,
      typeLabel: "Node",
      category: BT_NODES_KEY,
      kind: "behaviorNode",
      x: position.x,
      y: position.y,
      width: DEFAULT_CANVAS_NODE_WIDTH,
      height: DEFAULT_CANVAS_NODE_HEIGHT,
      hasOutgoing,
    };
  });

  const canvasNodeIdByGraphId = new Map<string, string>();
  for (const n of graph.nodes) {
    canvasNodeIdByGraphId.set(n.id, `bt-import-${n.id}`);
  }

  const rootNodeId =
    graph.rootId && canvasNodeIdByGraphId.has(graph.rootId)
      ? (canvasNodeIdByGraphId.get(graph.rootId) as string)
      : null;

  const connections = graph.edges
    .map((e): NodeConnection | null => {
      const sourceNodeId = canvasNodeIdByGraphId.get(e.sourceId);
      const targetNodeId = canvasNodeIdByGraphId.get(e.targetId);
      if (!sourceNodeId || !targetNodeId) return null;

      return {
        id: `bt-import-${e.id}`,
        sourceNodeId,
        targetNodeId,
        sourcePort: "bottom",
        targetPort: "top",
      };
    })
    .filter((x): x is NodeConnection => x !== null);

  return { nodes: canvasNodes, connections, rootNodeId };
}
