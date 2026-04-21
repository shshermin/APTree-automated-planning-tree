import type { CanvasNode, NodeConnection } from "../components/editor/types";
import {
  DEFAULT_CANVAS_NODE_HEIGHT,
  DEFAULT_CANVAS_NODE_WIDTH,
} from "../components/editor/types";
import {
  ACTION_INSTANCES_KEY,
  BT_NODES_KEY,
  DECORATOR_NODES_KEY,
  FLOW_NODES_KEY,
  SERVICE_NODES_KEY,
} from "../components/sidebar/utils/constants";
import {
  FLOW_SUCCESS_TYPES,
  type ActionInstance,
  type ActionType,
  type FlowSuccessType,
} from "../components/sidebar/utils/types";

export type AptreeValidateResponse = {
  ok?: boolean;
  treeName?: string;
  errors?: string[];
  findings?: string[];
  graph?: AptreeGraph;
  graphs?: AptreeGraph[];
  stderr?: string;
  toolLogs?: string;
};

export type AptreeGraph = {
  name?: string;
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
  paramNames?: string[];
  paramValues?: string[];
  subtreeRef?: string;
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
  payload: unknown,
): AptreeValidateResponse {
  if (!isObject(payload)) {
    return { ok: false, errors: ["Invalid response from backend"] };
  }

  const graph = isObject(payload.graph)
    ? normalizeGraph(payload.graph)
    : undefined;
  const graphs = Array.isArray(payload.graphs)
    ? payload.graphs
        .filter(isObject)
        .map((entry) => normalizeGraph(entry))
        .filter((entry): entry is AptreeGraph => !!entry)
    : undefined;

  return {
    ok: typeof payload.ok === "boolean" ? payload.ok : undefined,
    treeName:
      typeof payload.treeName === "string" ? payload.treeName : undefined,
    errors: Array.isArray(payload.errors)
      ? (payload.errors as string[])
      : undefined,
    findings: Array.isArray(payload.findings)
      ? (payload.findings as string[])
      : undefined,
    graph,
    graphs,
    stderr: typeof payload.stderr === "string" ? payload.stderr : undefined,
    toolLogs: typeof payload.toolLogs === "string" ? payload.toolLogs : undefined,
  };
}

function normalizeGraph(
  value: Record<string, unknown>,
): AptreeGraph | undefined {
  const name = typeof value.name === "string" ? value.name : undefined;

  const rootId =
    typeof value.rootId === "string"
      ? value.rootId
      : value.rootId === null
        ? null
        : null;

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
        paramNames: Array.isArray(node.paramNames)
          ? (node.paramNames.filter((v) => v != null).map((v) => String(v)) as string[])
          : undefined,
        paramValues: Array.isArray(node.paramValues)
          ? (node.paramValues.filter((v) => v != null).map((v) => String(v)) as string[])
          : undefined,
        subtreeRef: typeof node.subtreeRef === "string" ? node.subtreeRef : undefined,
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
    return { name, rootId, nodes: [], edges: [] };
  }

  return { name, rootId, nodes, edges };
}

type CanvasGraph = {
  nodes: CanvasNode[];
  connections: NodeConnection[];
  rootNodeId: string | null;
};

type PortName = "top" | "bottom" | "left" | "right";

/** A layout-ready graph that can be rendered independently (e.g. in SubtreeViewer). */
export type SubtreeLayout = {
  nodes: CanvasNode[];
  connections: NodeConnection[];
  rootNodeId: string | null;
  /** Action types discovered while laying out this subtree (not in the main sidebar). */
  discoveredActionTypes: ActionType[];
  /** Action instances discovered while laying out this subtree. */
  discoveredActionInstances: ActionInstance[];
};

/** Raw subtree entry stored for lazy layout computation. */
export type RawSubtreeEntry = { graph: AptreeGraph; index: number };

export type AptreeImportResult = {
  graph: CanvasGraph;
  /** Raw subtree graphs keyed by name — layouts are computed on demand via computeSubtreeLayout(). */
  rawSubtreeGraphs: Map<string, RawSubtreeEntry>;
  discoveredActionTypes: ActionType[];
  discoveredActionInstances: ActionInstance[];
};


/**
 * Processes multiple BehaviorTree graphs from a single .bt file.
 *
 * Graph[0] is the main tree placed on the canvas.
 * Graphs[1..N] are stored as separate SubtreeLayouts (not merged into the canvas)
 * so the browser only renders the top-level tree, keeping performance acceptable
 * for large files with many subtrees.
 */
export function aptreeGraphsToCanvasGraph(
  graphs: AptreeGraph[],
  actionTypes: ActionType[],
): AptreeImportResult {
  if (!graphs.length) {
    return {
      graph: { nodes: [], connections: [], rootNodeId: null },
      rawSubtreeGraphs: new Map(),
      discoveredActionTypes: [],
      discoveredActionInstances: [],
    };
  }

  const knownActionTypeIds = new Set(actionTypes.map((t) => t.id));
  const discoveredActionTypeMap = new Map<string, ActionType>();
  const discoveredActionInstanceMap = new Map<string, ActionInstance>();

  // ── Main graph (index 0) ──────────────────────────────────────────────────
  const mainResult = aptreeGraphToCanvasGraph(graphs[0]!, actionTypes);

  for (const t of mainResult.discoveredActionTypes) {
    if (!knownActionTypeIds.has(t.id)) discoveredActionTypeMap.set(t.id, t);
  }
  for (const inst of mainResult.discoveredActionInstances) {
    discoveredActionInstanceMap.set(inst.id, inst);
  }

  // Apply bt-tree-0- prefix so SubtreePanel can distinguish graph origin.
  const prefix0 = "bt-tree-0-";
  const mainIdMap = new Map(mainResult.graph.nodes.map((n) => [n.id, prefix0 + n.id]));
  const mainNodes = mainResult.graph.nodes.map((n) => ({ ...n, id: prefix0 + n.id }));
  const mainConnections = mainResult.graph.connections.map((c) => ({
    ...c,
    id: prefix0 + c.id,
    sourceNodeId: mainIdMap.get(c.sourceNodeId) ?? prefix0 + c.sourceNodeId,
    targetNodeId: mainIdMap.get(c.targetNodeId) ?? prefix0 + c.targetNodeId,
  }));
  const mainRootNodeId = mainResult.graph.rootNodeId
    ? prefix0 + mainResult.graph.rootNodeId
    : null;

  // ── Subtree graphs (index 1..N) — stored raw, layouts computed on demand ──
  const rawSubtreeGraphs = new Map<string, RawSubtreeEntry>();

  for (let i = 1; i < graphs.length; i++) {
    const subtreeGraph = graphs[i]!;
    if (subtreeGraph.name) {
      rawSubtreeGraphs.set(subtreeGraph.name, { graph: subtreeGraph, index: i });
    }
  }

  return {
    graph: { nodes: mainNodes, connections: mainConnections, rootNodeId: mainRootNodeId },
    rawSubtreeGraphs,
    discoveredActionTypes: Array.from(discoveredActionTypeMap.values()),
    discoveredActionInstances: Array.from(discoveredActionInstanceMap.values()),
  };
}

/**
 * Computes the layout for a single raw subtree graph on demand.
 * Call this lazily (e.g. when user selects a subtree or it starts ticking).
 */
export function computeSubtreeLayout(
  entry: RawSubtreeEntry,
  actionTypes: ActionType[],
): SubtreeLayout {
  const prefix = `bt-tree-${entry.index}-`;
  const result = aptreeGraphToCanvasGraph(entry.graph, actionTypes);
  const idMap = new Map(result.graph.nodes.map((n) => [n.id, prefix + n.id]));
  return {
    nodes: result.graph.nodes.map((n) => ({ ...n, id: prefix + n.id })),
    connections: result.graph.connections.map((c) => ({
      ...c,
      id: prefix + c.id,
      sourceNodeId: idMap.get(c.sourceNodeId) ?? prefix + c.sourceNodeId,
      targetNodeId: idMap.get(c.targetNodeId) ?? prefix + c.targetNodeId,
    })),
    rootNodeId: result.graph.rootNodeId ? prefix + result.graph.rootNodeId : null,
    discoveredActionTypes: result.discoveredActionTypes,
    discoveredActionInstances: result.discoveredActionInstances,
  };
}

export function aptreeGraphToCanvasGraph(
  graph: AptreeGraph,
  actionTypes: ActionType[],
): AptreeImportResult {
  const nodeById = new Map(graph.nodes.map((n) => [n.id, n] as const));

  const rootId =
    graph.rootId && nodeById.has(graph.rootId)
      ? graph.rootId
      : (graph.nodes.find((n) => n.kind === "flow")?.id ??
        graph.nodes[0]?.id ??
        null);

  const rootNode = rootId ? nodeById.get(rootId) : undefined;
  const rootIsFlow = rootNode?.kind === "flow";

  const edgesByKind = {
    contains: graph.edges.filter((e) => e.kind === "contains"),
    member: graph.edges.filter((e) => e.kind === "member"),
    relation: graph.edges.filter((e) => e.kind === "relation"),
    service: graph.edges.filter((e) => e.kind === "service"),
    decorator: graph.edges.filter((e) => e.kind === "decorator"),
    child: graph.edges.filter((e) => e.kind === "child"),
  } as const;

  const outgoingCountByNode = new Map<string, number>();
  for (const edge of graph.edges) {
    if (!edge.sourceId || !edge.targetId) continue;
    if (edge.kind === "member") continue;
    outgoingCountByNode.set(
      edge.sourceId,
      (outgoingCountByNode.get(edge.sourceId) ?? 0) + 1,
    );
  }

  const containsByOwner = new Map<string, string[]>();
  for (const e of edgesByKind.contains) {
    if (!e.sourceId || !e.targetId) continue;
    if (!containsByOwner.has(e.sourceId)) containsByOwner.set(e.sourceId, []);
    containsByOwner.get(e.sourceId)!.push(e.targetId);
  }

  const membersByGraph = new Map<string, string[]>();
  for (const e of edgesByKind.member) {
    if (!e.sourceId || !e.targetId) continue;
    if (!membersByGraph.has(e.sourceId)) membersByGraph.set(e.sourceId, []);
    membersByGraph.get(e.sourceId)!.push(e.targetId);
  }

  const servicesByFlow = new Map<string, string[]>();
  for (const e of edgesByKind.service) {
    if (!e.sourceId || !e.targetId) continue;
    if (!servicesByFlow.has(e.sourceId)) servicesByFlow.set(e.sourceId, []);
    servicesByFlow.get(e.sourceId)!.push(e.targetId);
  }

  const decoratorsByOwner = new Map<string, string[]>();
  for (const e of edgesByKind.decorator) {
    if (!e.sourceId || !e.targetId) continue;
    if (!decoratorsByOwner.has(e.sourceId))
      decoratorsByOwner.set(e.sourceId, []);
    decoratorsByOwner.get(e.sourceId)!.push(e.targetId);
  }

  const actionTypeNameMap = new Map(
    actionTypes.map((t) => [t.name.trim().toLowerCase(), t] as const),
  );

  const isValidFlowSuccessType = (
    value: string | undefined,
  ): value is FlowSuccessType =>
    !!value && (FLOW_SUCCESS_TYPES as readonly string[]).includes(value);

  const canvasNodes: CanvasNode[] = [];
  const canvasNodeIdByGraphId = new Map<string, string>();
  const canvasNodeById = new Map<string, CanvasNode>();

  // Track discovered action types that don't exist in the sidebar yet
  const discoveredActionTypes = new Map<string, ActionType>();
  // Track discovered action instances
  const discoveredActionInstances = new Map<string, ActionInstance>();

  for (const n of graph.nodes) {
    let canvasNode: CanvasNode | null = null;
    const hasOutgoing = (outgoingCountByNode.get(n.id) ?? 0) > 0;
    const normalizedAst = n.astType?.replace(/^AST/, "") ?? "";

    if (n.kind === "action") {
      const typeName = normalizedAst || n.label.split(" ")[0] || "Action";
      const resolved = actionTypeNameMap.get(typeName.trim().toLowerCase());

      const rawLabel = n.label ?? "";
      const argsMatch = rawLabel.match(/\(([^)]*)\)/);
      const labelArgs = argsMatch?.[1]
        ? argsMatch[1].trim().split(/\s+/).filter(Boolean)
        : undefined;
      const paramNames = n.paramNames ?? [];
      const paramValues = n.paramValues ?? labelArgs ?? [];

      // Determine or create action type
      let actionType = resolved;
      const typeId = resolved?.id ?? typeName;

      if (!resolved && !discoveredActionTypes.has(typeId)) {
        // Create new action type with generic property names
        const propertyCount = paramValues.length || paramNames.length;
        const newActionType: ActionType = {
          id: typeId,
          name: typeName,
          type: "GenericBTAction",
          properties: Array.from({ length: propertyCount }).map((_, index) => ({
            id: `param-${index + 1}`,
            name: paramNames[index]?.trim() || `param${index + 1}`,
            valueType: "string",
          })),
        };
        discoveredActionTypes.set(typeId, newActionType);
        actionType = newActionType;
      } else if (!resolved) {
        actionType = discoveredActionTypes.get(typeId);
      }

      // Create action instance with property values
      const instanceName = n.name ?? n.label;
      const instanceId = "bt-instance-" + n.id;

      // Map args to property values using the action type's property IDs
      const propertyValues: Record<string, string> = {};
      if (actionType && paramValues.length > 0) {
        if (paramNames.length > 0) {
          paramValues.forEach((value, index) => {
            const name = paramNames[index]?.trim().toLowerCase();
            const byName =
              name &&
              actionType.properties.find(
                (prop) => prop.name?.trim().toLowerCase() === name,
              );
            const prop = byName ?? actionType.properties[index];
            if (prop && value !== undefined) {
              propertyValues[prop.id] = value;
            }
          });
        } else {
          actionType.properties.forEach((prop, index) => {
            if (paramValues[index] !== undefined) {
              propertyValues[prop.id] = paramValues[index];
            }
          });
        }
      }

      const actionInstance: ActionInstance = {
        id: instanceId,
        name: instanceName,
        type: typeName, // Required by StructuredItem
        typeId: typeId,
        propertyValues,
      };
      discoveredActionInstances.set(instanceId, actionInstance);

      canvasNode = {
        id: "bt-import-" + n.id,
        sourceId: instanceId,
        name: instanceName,
        typeLabel: typeName,
        typeId: typeId,
        category: ACTION_INSTANCES_KEY,
        kind: "actionInstance",
        x: 0,
        y: 0,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        args: paramValues,
        hasOutgoing,
      };
    } else if (n.kind === "service") {
      canvasNode = {
        id: "bt-import-" + n.id,
        sourceId: n.name ?? n.id,
        name: "",
        typeLabel: n.label,
        category: SERVICE_NODES_KEY,
        kind: "behaviorNode",
        x: 0,
        y: 0,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        hasOutgoing: false,
      };
    } else if (n.kind === "decorator") {
      canvasNode = {
        id: "bt-import-" + n.id,
        sourceId: n.name ?? n.id,
        name: n.name ?? n.label,
        typeLabel: "Decorator",
        category: DECORATOR_NODES_KEY,
        kind: "behaviorNode",
        x: 0,
        y: 0,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        hasOutgoing,
      };
    } else if (n.kind === "flow") {
      const successType = isValidFlowSuccessType(n.successType)
        ? n.successType
        : undefined;

      canvasNode = {
        id: "bt-import-" + n.id,
        sourceId: n.name ?? n.id,
        name: n.name ?? n.label,
        typeLabel: "Flow",
        category: FLOW_NODES_KEY,
        kind: "behaviorNode",
        x: 0,
        y: 0,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        successType,
        hasOutgoing,
      };
    } else if (n.kind === "btNode") {
      canvasNode = {
        id: "bt-import-" + n.id,
        sourceId: n.name ?? n.id,
        name: n.name ?? n.label,
        typeLabel: "Node",
        category: BT_NODES_KEY,
        kind: "behaviorNode",
        x: 0,
        y: 0,
        width: DEFAULT_CANVAS_NODE_WIDTH,
        height: DEFAULT_CANVAS_NODE_HEIGHT,
        hasOutgoing,
      };
    }

    if (canvasNode) {
      canvasNodes.push(canvasNode);
      canvasNodeIdByGraphId.set(n.id, canvasNode.id);
      canvasNodeById.set(canvasNode.id, canvasNode);
    }
  }

  // Layout constants
  const ROOT_X = 0;
  const ROOT_Y = -150; // Position root above the outer nodegraph
  const ROOT_TO_FLOW_GAP_Y = 220;
  const FLOW_TO_SUBTREE_GAP_Y = 200;
  const SERVICE_FLOW_EXTRA_GAP_Y = 60;
  const TREE_GAP_X = 320; // Horizontal gap for plain BT (child-edge) layout
  const TREE_GAP_Y = 120; // Vertical gap for plain BT (child-edge) layout
  const SERVICE_OFFSET_Y = 55; // Service box offset above flow node

  const positionDecorators = () => {
    for (const [ownerId, decoratorIds] of decoratorsByOwner) {
      const ownerCanvasId = canvasNodeIdByGraphId.get(ownerId);
      if (!ownerCanvasId) continue;
      const ownerNode = canvasNodeById.get(ownerCanvasId);
      if (!ownerNode) continue;

      const ownerW = ownerNode.width ?? DEFAULT_CANVAS_NODE_WIDTH;
      const ownerH = ownerNode.height ?? DEFAULT_CANVAS_NODE_HEIGHT;
      const decoratorOffsetY = Math.max(60, ownerH * 0.6 + 20);
      const decoratorGapX = Math.max(160, ownerW * 0.7 + 40);

      for (let i = 0; i < decoratorIds.length; i++) {
        const did = decoratorIds[i];
        const decCanvasId = canvasNodeIdByGraphId.get(did);
        if (!decCanvasId) continue;
        const decNode = canvasNodeById.get(decCanvasId);
        if (!decNode) continue;

        // Spread multiple decorators around the owner (0, +1, -1, +2, -2, ...)
        const k = i === 0 ? 0 : Math.ceil(i / 2);
        const sign = i % 2 === 1 ? 1 : -1;
        const dx = i === 0 ? 0 : sign * k * decoratorGapX;

        decNode.x = ownerNode.x + dx;
        decNode.y = ownerNode.y - decoratorOffsetY;
      }
    }
  };

  const isMeets = (e: AptreeGraphEdge) => {
    if (e.kind !== "relation") return false;
    const label = typeof e.label === "string" ? e.label : "";
    return label.toLowerCase().includes("meet");
  };

  const nodeBounds = (node: CanvasNode) => {
    const w = node.width ?? DEFAULT_CANVAS_NODE_WIDTH;
    const h = node.height ?? DEFAULT_CANVAS_NODE_HEIGHT;
    return {
      left: node.x - w / 2,
      top: node.y - h / 2,
      right: node.x + w / 2,
      bottom: node.y + h / 2,
    };
  };

  const unionBounds = (
    boundsList: Array<{
      left: number;
      top: number;
      right: number;
      bottom: number;
    }>,
  ) => {
    let left = Number.POSITIVE_INFINITY;
    let top = Number.POSITIVE_INFINITY;
    let right = Number.NEGATIVE_INFINITY;
    let bottom = Number.NEGATIVE_INFINITY;
    for (const b of boundsList) {
      left = Math.min(left, b.left);
      top = Math.min(top, b.top);
      right = Math.max(right, b.right);
      bottom = Math.max(bottom, b.bottom);
    }
    return { left, top, right, bottom };
  };

  const wrapperNodes: CanvasNode[] = [];
  const connections: NodeConnection[] = [];

  const portPoint = (node: CanvasNode, port: PortName) => {
    const halfW = (node.width ?? DEFAULT_CANVAS_NODE_WIDTH) / 2;
    const halfH = (node.height ?? DEFAULT_CANVAS_NODE_HEIGHT) / 2;
    switch (port) {
      case "left":
        return { x: node.x - halfW, y: node.y };
      case "right":
        return { x: node.x + halfW, y: node.y };
      case "top":
        return { x: node.x, y: node.y - halfH };
      case "bottom":
        return { x: node.x, y: node.y + halfH };
    }
  };

  const chooseShortestPorts = (source: CanvasNode, target: CanvasNode) => {
    const ports: PortName[] = ["top", "bottom", "left", "right"];
    let best: {
      sourcePort: PortName;
      targetPort: PortName;
      d2: number;
    } | null = null;
    for (const sp of ports) {
      const a = portPoint(source, sp);
      for (const tp of ports) {
        const b = portPoint(target, tp);
        const dx = a.x - b.x;
        const dy = a.y - b.y;
        const d2 = dx * dx + dy * dy;
        if (!best || d2 < best.d2) {
          best = { sourcePort: sp, targetPort: tp, d2 };
        }
      }
    }
    return (
      best ?? {
        sourcePort: "bottom" as const,
        targetPort: "top" as const,
        d2: 0,
      }
    );
  };

  const chooseMeetsPorts = (
    source: CanvasNode,
    target: CanvasNode,
  ): { sourcePort: PortName; targetPort: PortName } => {
    const dx = target.x - source.x;
    const dy = target.y - source.y;
    const sameRowTolerance = 48;

    // Same row: connect side-to-side.
    if (Math.abs(dy) <= sameRowTolerance) {
      return dx >= 0
        ? { sourcePort: "right", targetPort: "left" }
        : { sourcePort: "left", targetPort: "right" };
    }

    // Different rows: connect top/bottom to avoid "floating" center lines.
    if (dy >= 0) {
      return { sourcePort: "bottom", targetPort: "top" };
    }
    return { sourcePort: "top", targetPort: "bottom" };
  };

  // Track info for each flow
  const flowInfo = new Map<
    string,
    {
      flowX: number;
      flowY: number;
      subtreeId: string;
      serviceWrapperId?: string;
      subtreeCenterX: number;
      subtreeCenterY: number;
      subtreeWidth: number;
      subtreeHeight: number;
    }
  >();

  const getActionMembersForFlow = (flowId: string): CanvasNode[] => {
    const contained = containsByOwner.get(flowId) ?? [];
    const actionGraphIds = contained.filter(
      (id) => nodeById.get(id)?.kind === "nodeGraph",
    );
    const actionGraphId = actionGraphIds[0];

    const actionMembers: CanvasNode[] = [];
    if (actionGraphId) {
      const memberIds = membersByGraph.get(actionGraphId) ?? [];
      for (const mid of memberIds) {
        const canvasId = canvasNodeIdByGraphId.get(mid);
        if (canvasId) {
          const cn = canvasNodeById.get(canvasId);
          if (
            cn &&
            (nodeById.get(mid)?.kind === "action" ||
              nodeById.get(mid)?.kind === "btNode")
          ) {
            actionMembers.push(cn);
          }
        }
      }
    }
    return actionMembers;
  };

  const estimateFlowWidth = (flowId: string): number => {
    const actionMembers = getActionMembersForFlow(flowId);
    const maxActionW = actionMembers.length
      ? Math.max(
          ...actionMembers.map((n) => n.width ?? DEFAULT_CANVAS_NODE_WIDTH),
        )
      : DEFAULT_CANVAS_NODE_WIDTH;
    const maxActionH = actionMembers.length
      ? Math.max(
          ...actionMembers.map((n) => n.height ?? DEFAULT_CANVAS_NODE_HEIGHT),
        )
      : DEFAULT_CANVAS_NODE_HEIGHT;

    const actionColGapX = Math.max(maxActionW * 1.2 + 80, 320);
    const padX = Math.max(24, maxActionW * 0.15 + 20);
    const hasPair = (() => {
      const actionIds = actionMembers
        .map((n) => {
          for (const [gid, cid] of canvasNodeIdByGraphId) {
            if (cid === n.id) return gid;
          }
          return "";
        })
        .filter(Boolean);
      if (!actionIds.length) return false;
      const actionIdSet = new Set(actionIds);
      return edgesByKind.relation.some(
        (e) =>
          isMeets(e) &&
          actionIdSet.has(e.sourceId) &&
          actionIdSet.has(e.targetId),
      );
    })();

    const actionWidth = hasPair ? actionColGapX + maxActionW : maxActionW;
    let subtreeWidth = actionWidth + padX * 2;

    const hasService = (servicesByFlow.get(flowId) ?? []).length > 0;
    if (hasService) {
      const serviceOuterPadding = 20;
      subtreeWidth += serviceOuterPadding * 2;
    }

    // Keep a sensible minimum footprint
    return Math.max(subtreeWidth, DEFAULT_CANVAS_NODE_WIDTH * 1.6, maxActionH);
  };

  // Layout a single flow node and its contained actions
  const layoutFlowAndActions = (
    flowId: string,
    baseX: number,
    flowY: number,
  ) => {
    const flowCanvasId = canvasNodeIdByGraphId.get(flowId);
    const flowCanvasNode = flowCanvasId
      ? canvasNodeById.get(flowCanvasId)
      : undefined;
    if (!flowCanvasNode) return;

    // Position flow node ABOVE the subtree
    flowCanvasNode.x = baseX;
    flowCanvasNode.y = flowY;

    // Position service ABOVE flow node
    const serviceIds = servicesByFlow.get(flowId) ?? [];
    const hasService = serviceIds.length > 0;
    if (hasService) {
      const serviceId = serviceIds[0]!;
      const serviceCanvasId = canvasNodeIdByGraphId.get(serviceId);
      const serviceCanvasNode = serviceCanvasId
        ? canvasNodeById.get(serviceCanvasId)
        : undefined;
      if (serviceCanvasNode) {
        serviceCanvasNode.x = baseX;
        serviceCanvasNode.y = flowY - SERVICE_OFFSET_Y;
        flowCanvasNode.serviceLabel = serviceCanvasNode.typeLabel;
        serviceCanvasNode.hidden = true;
      }
    }

    const actionMembers = getActionMembersForFlow(flowId);

    // Build MEETS pairs for action layout
    const actionIds = actionMembers
      .map((n) => {
        for (const [gid, cid] of canvasNodeIdByGraphId) {
          if (cid === n.id) return gid;
        }
        return "";
      })
      .filter(Boolean);
    const actionIdSet = new Set(actionIds);

    const pairs = edgesByKind.relation
      .filter(
        (e) =>
          isMeets(e) &&
          actionIdSet.has(e.sourceId) &&
          actionIdSet.has(e.targetId),
      )
      .map((e) => ({ source: e.sourceId, target: e.targetId }));

    const used = new Set<string>();
    const rows: Array<{ left: string; right: string } | { single: string }> =
      [];

    for (const p of pairs) {
      if (used.has(p.source) || used.has(p.target)) continue;
      used.add(p.source);
      used.add(p.target);
      rows.push({ left: p.source, right: p.target });
    }

    for (const id of actionIds) {
      if (used.has(id)) continue;
      used.add(id);
      rows.push({ single: id });
    }

    const maxActionW = actionMembers.length
      ? Math.max(
          ...actionMembers.map((n) => n.width ?? DEFAULT_CANVAS_NODE_WIDTH),
        )
      : DEFAULT_CANVAS_NODE_WIDTH;
    const maxActionH = actionMembers.length
      ? Math.max(
          ...actionMembers.map((n) => n.height ?? DEFAULT_CANVAS_NODE_HEIGHT),
        )
      : DEFAULT_CANVAS_NODE_HEIGHT;

    // Dynamic spacing based on action size (+ small constant)
    // actionRowGapY must be >= wrapper height (node + 45 top pad + 20 bottom pad) + gap
    const actionColGapX = Math.max(maxActionW * 1.2 + 80, 320);
    const actionRowGapY = Math.max(maxActionH + 90, 270);

    // Calculate action positions - starting below the flow node
    const actionStartY =
      flowY +
      FLOW_TO_SUBTREE_GAP_Y +
      (hasService ? SERVICE_FLOW_EXTRA_GAP_Y : 0) +
      Math.max(60, maxActionH * 0.4 + 20);
    const leftX = baseX - actionColGapX / 2;
    const rightX = baseX + actionColGapX / 2;

    for (let i = 0; i < rows.length; i++) {
      const y = actionStartY + i * actionRowGapY;
      const row = rows[i];
      if ("single" in row) {
        const canvasId = canvasNodeIdByGraphId.get(row.single);
        if (canvasId) {
          const cn = canvasNodeById.get(canvasId);
          if (cn) {
            cn.x = baseX;
            cn.y = y;
          }
        }
      } else {
        const leftCanvasId = canvasNodeIdByGraphId.get(row.left);
        const rightCanvasId = canvasNodeIdByGraphId.get(row.right);
        if (leftCanvasId) {
          const cn = canvasNodeById.get(leftCanvasId);
          if (cn) {
            cn.x = leftX;
            cn.y = y;
          }
        }
        if (rightCanvasId) {
          const cn = canvasNodeById.get(rightCanvasId);
          if (cn) {
            cn.x = rightX;
            cn.y = y;
          }
        }
      }
    }

    // Create SubTreeInject wrappers for EACH action node (not one big wrapper)
    const actionWrapperIds: string[] = [];
    for (const actionNode of actionMembers) {
      const actionBounds = nodeBounds(actionNode);
      const actionPadded = {
        left: actionBounds.left - 20,
        right: actionBounds.right + 20,
        top: actionBounds.top - 45,
        bottom: actionBounds.bottom + 20,
      };
      const actionWrapperWidth = actionPadded.right - actionPadded.left;
      const actionWrapperHeight = actionPadded.bottom - actionPadded.top;
      const actionWrapperCenterX = actionPadded.left + actionWrapperWidth / 2;
      const actionWrapperCenterY = actionPadded.top + actionWrapperHeight / 2;

      const actionWrapperId =
        "bt-subtree-inject-" + flowId + "-" + actionNode.id;
      const actionWrapper: CanvasNode = {
        id: actionWrapperId,
        sourceId: "subtree-inject-" + flowId + "-" + actionNode.id,
        name: "",
        typeLabel: "SubTreeInject",
        category: BT_NODES_KEY,
        kind: "behaviorNode",
        x: actionWrapperCenterX,
        y: actionWrapperCenterY,
        width: actionWrapperWidth,
        height: actionWrapperHeight,
        renderAsSubtree: true,
        subtreeTitle: "",
        hasOutgoing: true,
      };
      wrapperNodes.push(actionWrapper);
      actionWrapperIds.push(actionWrapperId);
    }

    // Calculate overall bounds for all actions (for Planning Service wrapper)
    const actionBounds = actionMembers.length
      ? unionBounds(actionMembers.map((n) => nodeBounds(n)))
      : {
          left: baseX - 100,
          right: baseX + 100,
          top: actionStartY - 60,
          bottom: actionStartY + 60,
        };

    const padX = Math.max(24, maxActionW * 0.15 + 20);
    const padTop = Math.max(32, maxActionH * 0.25 + 20);
    const padBottom = Math.max(28, maxActionH * 0.2 + 18);

    const subtreePadded = {
      left: actionBounds.left - padX,
      right: actionBounds.right + padX,
      top: actionBounds.top - padTop,
      bottom: actionBounds.bottom + padBottom,
    };

    const subtreeWidth = subtreePadded.right - subtreePadded.left;
    const subtreeHeight = subtreePadded.bottom - subtreePadded.top;
    const subtreeCenterX = subtreePadded.left + subtreeWidth / 2;
    const subtreeCenterY = subtreePadded.top + subtreeHeight / 2;

    // Center the flow (and service) horizontally over the subtree
    flowCanvasNode.x = subtreeCenterX;
    if (hasService) {
      const serviceId = serviceIds[0]!;
      const serviceCanvasId = canvasNodeIdByGraphId.get(serviceId);
      const serviceCanvasNode = serviceCanvasId
        ? canvasNodeById.get(serviceCanvasId)
        : undefined;
      if (serviceCanvasNode) {
        serviceCanvasNode.x = subtreeCenterX;
        serviceCanvasNode.y = flowY - SERVICE_OFFSET_Y;
        flowCanvasNode.serviceLabel = serviceCanvasNode.typeLabel;
        serviceCanvasNode.hidden = true;
      }
    }

    // Create a nodegraph wrapper for the whole action group (so it shows even without services)
    const subtreeWrapperId = "bt-nodegraph-wrapper-" + flowId;
    const subtreeWrapper: CanvasNode = {
      id: subtreeWrapperId,
      sourceId: "nodegraph-wrapper-" + flowId,
      name: "",
      typeLabel: "",
      category: BT_NODES_KEY,
      kind: "behaviorNode",
      x: subtreeCenterX,
      y: subtreeCenterY,
      width: subtreeWidth,
      height: subtreeHeight,
      renderAsSubtree: true,
      subtreeTitle: "",
      hasOutgoing: true,
    };
    wrapperNodes.push(subtreeWrapper);

    // No extra planning-service wrapper; keep only nodegraph + per-action SubTreeInject wrappers
    const serviceWrapperId: string | undefined = undefined;

    flowInfo.set(flowId, {
      flowX: baseX,
      flowY,
      subtreeId: subtreeWrapperId,
      serviceWrapperId,
      subtreeCenterX,
      subtreeCenterY,
      subtreeWidth,
      subtreeHeight,
    });

    // Connection from flow node down to its (service) subtree wrapper.
    const targetSubtree = subtreeWrapperId;
    if (flowCanvasId) {
      connections.push({
        id: "bt-flow-to-subtree-" + flowId,
        sourceNodeId: flowCanvasId,
        targetNodeId: targetSubtree,
        sourcePort: "bottom",
        targetPort: "top",
        kind: "contains",
      });
    }

    // Add MEETS connections between actions
    for (const rel of edgesByKind.relation) {
      if (!isMeets(rel)) continue;
      if (actionIdSet.has(rel.sourceId) && actionIdSet.has(rel.targetId)) {
        const sourceCanvasId = canvasNodeIdByGraphId.get(rel.sourceId);
        const targetCanvasId = canvasNodeIdByGraphId.get(rel.targetId);
        if (sourceCanvasId && targetCanvasId) {
          const sourceNode = canvasNodeById.get(sourceCanvasId);
          const targetNode = canvasNodeById.get(targetCanvasId);
          if (!sourceNode || !targetNode) continue;
          const ports = chooseMeetsPorts(sourceNode, targetNode);
          connections.push({
            id: "bt-import-action-meet-" + rel.id,
            sourceNodeId: sourceCanvasId,
            targetNodeId: targetCanvasId,
            sourcePort: ports.sourcePort,
            targetPort: ports.targetPort,
            kind: "relation",
            label: rel.label,
          });
        }
      }
    }
  };

  // Find outer nodeGraph (child of root)
  const rootGraphIds = rootId
    ? (containsByOwner.get(rootId) ?? []).filter(
        (id) => nodeById.get(id)?.kind === "nodeGraph",
      )
    : [];
  const outerGraphId = rootGraphIds[0];

  // Get flow nodes inside outer nodeGraph
  const flowIds = outerGraphId
    ? (membersByGraph.get(outerGraphId) ?? []).filter(
        (id) => nodeById.get(id)?.kind === "flow",
      )
    : [];

  // Place root node
  if (rootId) {
    const rootCanvasId = canvasNodeIdByGraphId.get(rootId);
    if (rootCanvasId) {
      const rootNode = canvasNodeById.get(rootCanvasId);
      if (rootNode) {
        rootNode.x = ROOT_X;
        rootNode.y = ROOT_Y;
      }
    }
  }

  if (flowIds.length > 0) {
    // Arrange flow nodes in a row with dynamic spacing based on subtree widths
    const flowWidths = flowIds.map((fid) => ({
      id: fid,
      width: estimateFlowWidth(fid),
    }));
    const baseGap = Math.max(80, DEFAULT_CANVAS_NODE_WIDTH * 0.4);
    const totalWidth =
      flowWidths.reduce((sum, f) => sum + f.width, 0) +
      baseGap * Math.max(0, flowWidths.length - 1);
    let cursorX = ROOT_X - totalWidth / 2;
    const flowY = ROOT_Y + ROOT_TO_FLOW_GAP_Y;

    flowWidths.forEach((entry) => {
      const centerX = cursorX + entry.width / 2;
      layoutFlowAndActions(entry.id, centerX, flowY);
      cursorX += entry.width + baseGap;
    });

    // After placing flows/actions, place any decorator nodes attached to them.
    positionDecorators();
  } else if (rootIsFlow && rootId) {
    // Subtree graphs often have a single root flow that directly contains an action nodeGraph.
    // Those graphs don't have an "outer" nodeGraph with member flow nodes, so the normal
    // multi-flow layout above won't run. Without a dedicated path, nodes keep their default
    // (0,0) positions and overlap.
    layoutFlowAndActions(rootId, ROOT_X, ROOT_Y);

    // After placing flow/actions, place any decorator nodes attached to them.
    positionDecorators();
  } else if (edgesByKind.child.length > 0 && rootId) {
    // Fallback: plain BehaviorTree layout based on `child` edges.
    const childrenByParent = new Map<string, string[]>();
    for (const e of edgesByKind.child) {
      if (!e.sourceId || !e.targetId) continue;
      if (!childrenByParent.has(e.sourceId))
        childrenByParent.set(e.sourceId, []);
      childrenByParent.get(e.sourceId)!.push(e.targetId);
    }

    const tempYById = new Map<string, number>();
    const depthById = new Map<string, number>();
    let nextY = 0;

    const setNodePos = (graphId: string, x: number, y: number) => {
      const canvasId = canvasNodeIdByGraphId.get(graphId);
      if (!canvasId) return;
      const cn = canvasNodeById.get(canvasId);
      if (!cn) return;
      cn.x = x;
      cn.y = y;
    };

    const layoutSubtree = (graphId: string, depth: number): number => {
      const children = childrenByParent.get(graphId) ?? [];

      depthById.set(graphId, depth);

      // Set X immediately so nodes don't pile up even if something is missing.
      setNodePos(graphId, ROOT_X + depth * TREE_GAP_X, 0);

      if (children.length === 0) {
        const canvasId = canvasNodeIdByGraphId.get(graphId);
        const cn = canvasId ? canvasNodeById.get(canvasId) : undefined;
        const h = cn?.height ?? DEFAULT_CANVAS_NODE_HEIGHT;
        const y = nextY;
        nextY += Math.max(TREE_GAP_Y, h + 40);
        tempYById.set(graphId, y);
        return y;
      }

      const childYs = children.map((childId) =>
        layoutSubtree(childId, depth + 1),
      );
      const minY = Math.min(...childYs);
      const maxY = Math.max(...childYs);
      const y = (minY + maxY) / 2;
      tempYById.set(graphId, y);
      return y;
    };

    layoutSubtree(rootId, 0);
    const rootTempY = tempYById.get(rootId) ?? 0;
    const yShift = ROOT_Y - rootTempY;
    for (const [graphId, y] of tempYById) {
      const canvasId = canvasNodeIdByGraphId.get(graphId);
      if (!canvasId) continue;
      const cn = canvasNodeById.get(canvasId);
      if (!cn) continue;
      const depth = depthById.get(graphId) ?? 0;
      cn.x = ROOT_X + depth * TREE_GAP_X;
      cn.y = y + yShift;
    }

    // Center parents between their direct children (symmetric connectors)
    for (const [parentId, children] of childrenByParent) {
      if (children.length < 2) continue;
      const parentCanvasId = canvasNodeIdByGraphId.get(parentId);
      const parentNode = parentCanvasId ? canvasNodeById.get(parentCanvasId) : undefined;
      if (!parentNode) continue;
      const childYs = children
        .map((cid) => tempYById.get(cid))
        .filter((val): val is number => typeof val === "number");
      if (!childYs.length) continue;
      const minY = Math.min(...childYs);
      const maxY = Math.max(...childYs);
      parentNode.y = (minY + maxY) / 2 + yShift;
    }

    // Decorator nodes are connected via `decorator` edges and don't participate in the
    // `child`-tree layout, so we position them relative to their owning node here.
    positionDecorators();
  }

  // Add all wrapper nodes to canvas
  canvasNodes.push(...wrapperNodes);

  // Create outer subtree container (the dashed box containing everything)
  if (flowIds.length > 0) {
    const allBounds: {
      left: number;
      top: number;
      right: number;
      bottom: number;
    }[] = [];

    // Include flow nodes
    for (const fid of flowIds) {
      const flowCanvasId = canvasNodeIdByGraphId.get(fid);
      if (flowCanvasId) {
        const fn = canvasNodeById.get(flowCanvasId);
        if (fn) {
          allBounds.push(nodeBounds(fn));
        }
      }
      // Include service nodes
      const serviceIds = servicesByFlow.get(fid) ?? [];
      for (const sid of serviceIds) {
        const scid = canvasNodeIdByGraphId.get(sid);
        if (scid) {
          const sn = canvasNodeById.get(scid);
          if (sn && !sn.hidden) {
            allBounds.push(nodeBounds(sn));
          }
        }
      }
      // Include subtree bounds
      const info = flowInfo.get(fid);
      if (info) {
        const w = info.serviceWrapperId
          ? info.subtreeWidth + 40
          : info.subtreeWidth;
        const h = info.serviceWrapperId
          ? info.subtreeHeight + 40
          : info.subtreeHeight;
        allBounds.push({
          left: info.subtreeCenterX - w / 2,
          right: info.subtreeCenterX + w / 2,
          top: info.subtreeCenterY - h / 2,
          bottom: info.subtreeCenterY + h / 2,
        });
      }
    }

    if (allBounds.length) {
      const combined = unionBounds(allBounds);
      const outerPadded = {
        left: combined.left - 40,
        right: combined.right + 40,
        top: combined.top - 40,
        bottom: combined.bottom + 30,
      };

      const outerWidth = outerPadded.right - outerPadded.left;
      const outerHeight = outerPadded.bottom - outerPadded.top;
      const outerCenterX = outerPadded.left + outerWidth / 2;
      const outerCenterY = outerPadded.top + outerHeight / 2;

      // Reposition root node to be centered above the outer subtree
      if (rootId) {
        const rootCanvasId = canvasNodeIdByGraphId.get(rootId);
        if (rootCanvasId) {
          const rootNode = canvasNodeById.get(rootCanvasId);
          if (rootNode) {
            rootNode.x = outerCenterX;
          }
        }
      }

      const outerSubtree: CanvasNode = {
        id: "bt-outer-subtree",
        sourceId: "outer-subtree",
        name: "",
        typeLabel: "",
        category: BT_NODES_KEY,
        kind: "behaviorNode",
        x: outerCenterX,
        y: outerCenterY,
        width: outerWidth,
        height: outerHeight,
        renderAsSubtree: true,
        subtreeTitle: "",
        hasOutgoing: false,
      };
      canvasNodes.push(outerSubtree);

      // Connection from root to outer subtree
      if (rootId) {
        const rootCanvasId = canvasNodeIdByGraphId.get(rootId);
        if (rootCanvasId) {
          connections.push({
            id: "bt-import-root-to-outer",
            sourceNodeId: rootCanvasId,
            targetNodeId: "bt-outer-subtree",
            sourcePort: "bottom",
            targetPort: "top",
            kind: "contains",
          });
        }
      }
    }
  }

  // Add MEETS connections between flow nodes (from source data)
  for (const rel of edgesByKind.relation) {
    if (!isMeets(rel)) continue;
    const sourceInFlows = flowIds.includes(rel.sourceId);
    const targetInFlows = flowIds.includes(rel.targetId);

    if (sourceInFlows && targetInFlows) {
      const sourceInfo = flowInfo.get(rel.sourceId);
      const targetInfo = flowInfo.get(rel.targetId);
      if (sourceInfo && targetInfo) {
        const sourceWrapperId =
          sourceInfo.serviceWrapperId ?? sourceInfo.subtreeId;
        const targetWrapperId =
          targetInfo.serviceWrapperId ?? targetInfo.subtreeId;

        connections.push({
          id: "bt-import-flow-meet-" + rel.id,
          sourceNodeId: sourceWrapperId,
          targetNodeId: targetWrapperId,
          sourcePort: sourceInfo.subtreeCenterX <= targetInfo.subtreeCenterX ? "right" : "left",
          targetPort: sourceInfo.subtreeCenterX <= targetInfo.subtreeCenterX ? "left" : "right",
          kind: "relation",
          label: rel.label,
        });
      }
    }
  }

  // Add remaining edges
  for (const e of graph.edges) {
    if (
      e.kind === "member" ||
      e.kind === "contains" ||
      e.kind === "service" ||
      e.kind === "relation"
    ) {
      continue;
    }

    const sourceCanvasId = canvasNodeIdByGraphId.get(e.sourceId);
    const targetCanvasId = canvasNodeIdByGraphId.get(e.targetId);
    if (!sourceCanvasId || !targetCanvasId) continue;

    connections.push({
      id: "bt-import-" + e.id,
      sourceNodeId: sourceCanvasId,
      targetNodeId: targetCanvasId,
      sourcePort: "bottom",
      targetPort: "top",
      kind: e.kind,
      label: e.label,
    });
  }

  // Post-process: choose ports that minimize the distance between connected nodes.
  // This reduces "detours" in edge routing when our initial port defaults don't match geometry.
  // Run this at the very end so it also covers wrapper nodes (subtrees/services/outer) we added.
  const allNodesById = new Map(canvasNodes.map((n) => [n.id, n] as const));
  for (const c of connections) {
    if (c.kind === "relation") continue;
    const s = allNodesById.get(c.sourceNodeId);
    const t = allNodesById.get(c.targetNodeId);
    if (!s || !t) continue;
    const best = chooseShortestPorts(s, t);
    c.sourcePort = best.sourcePort;
    c.targetPort = best.targetPort;
  }

  const rootNodeId =
    graph.rootId && canvasNodeIdByGraphId.has(graph.rootId)
      ? (canvasNodeIdByGraphId.get(graph.rootId) as string)
      : null;

  return {
    graph: { nodes: canvasNodes, connections, rootNodeId },
    rawSubtreeGraphs: new Map(),
    discoveredActionTypes: Array.from(discoveredActionTypes.values()),
    discoveredActionInstances: Array.from(discoveredActionInstances.values()),
  };
}
