import { useEffect, useMemo, useRef, useState } from "react";
import EditorCanvas from "./EditorCanvas";
import type { CanvasNode, NodeConnection } from "./types";
import type { ActionInstance, ActionType } from "../sidebar/utils/types";
import { ACTION_INSTANCES_KEY, FLOW_NODES_KEY } from "../sidebar/utils/constants";
import "./SubtreePanel.css";

/* ── Types ── */
interface SubtreePanelProps {
  nodes: CanvasNode[];
  connections: NodeConnection[];
  tickStatus: Record<string, string>;
  actionTypes?: ActionType[];
  actionInstances?: ActionInstance[];
}


function findActiveSubtree(
  nodes: CanvasNode[],
  connections: NodeConnection[],
  tickStatus: Record<string, string>,
): { flowNode: CanvasNode | null; subtreeNodes: CanvasNode[]; subtreeConnections: NodeConnection[] } {
  if (Object.keys(tickStatus).length === 0)
    return { flowNode: null, subtreeNodes: [], subtreeConnections: [] };

  // DEBUG: Log tick status and available node names for diagnosis
  const tickNames = Object.keys(tickStatus);
  if (tickNames.length > 0) {
    const nodeNames = nodes.filter((n) => n.name).map((n) => `${n.name} [${n.category}]`);
    console.log("[SubtreePanel] tickStatus keys:", tickNames);
    console.log("[SubtreePanel] canvas node names:", nodeNames.slice(0, 20));
    console.log("[SubtreePanel] total nodes:", nodes.length, "total connections:", connections.length);
  }

  const nodeByName = new Map(nodes.filter((n) => n.name).map((n) => [n.name, n]));
  const nodeById   = new Map(nodes.map((n) => [n.id, n]));

  // Pre-compute which FlowNode IDs are children of another FlowNode (have an
  // incoming "contains" connection).  These are the layer-level FlowNodes such
  // as Layers1_2, Layers3_4, etc.  Root FlowNodes (Main) and subtree-root
  // FlowNodes (ENHSP_Demonstrator_DynamicFlow_…) are NOT children and should
  // only be used as a fallback.
  const childFlowNodeIds = new Set<string>();
  for (const conn of connections) {
    if (conn.kind === "contains") {
      const target = nodeById.get(conn.targetNodeId);
      if (target?.category === FLOW_NODES_KEY) {
        childFlowNodeIds.add(target.id);
      }
    }
  }

  // 1. Try to find a running FlowNode directly in tickStatus – that gives us the
  //    most stable anchor (doesn't change on every action tick within the same layer).
  //    Prefer child FlowNodes (layer-level) over root / subtree-root FlowNodes.
  let flowNode: CanvasNode | null = null;
  let fallbackFlowNode: CanvasNode | null = null;
  for (const [name, status] of Object.entries(tickStatus)) {
    if (status !== "Running") continue;
    const n = nodeByName.get(name);
    if (n?.category === FLOW_NODES_KEY) {
      if (childFlowNodeIds.has(n.id)) {
        flowNode = n;
        break;
      }
      if (!fallbackFlowNode) {
        fallbackFlowNode = n;
      }
    }
  }
  if (!flowNode) flowNode = fallbackFlowNode;
  console.log("[SubtreePanel] Step 1 result: flowNode =", flowNode?.name ?? "null", "| childFlowNodeIds:", [...childFlowNodeIds].length);

  // 2. If no running FlowNode is in tickStatus, find the running action node and
  //    walk up via "contains" connections to its parent FlowNode.
  if (!flowNode) {
    let actionNode: CanvasNode | undefined;
    for (const [name, status] of Object.entries(tickStatus)) {
      if (status !== "Running") continue;
      const n = nodeByName.get(name);
      console.log("[SubtreePanel] Step 2: checking tick name", name, "-> found?", !!n, n?.category);
      if (n && (n.category === ACTION_INSTANCES_KEY || n.kind === "actionInstance")) {
        actionNode = n; break;
      }
    }
    // Fallback: any ticked node
    if (!actionNode) {
      for (const name of Object.keys(tickStatus)) {
        const n = nodeByName.get(name);
        if (n) { actionNode = n; break; }
      }
    }
    if (!actionNode) return { flowNode: null, subtreeNodes: [], subtreeConnections: [] };

    // Walk "contains" edges upward until we hit a FlowNode.
    // Prefer child FlowNodes (layer-level) over subtree-root FlowNodes.
    let walkFallback: CanvasNode | null = null;
    const visited = new Set<string>();
    const queue = [actionNode.id];
    while (queue.length > 0 && !flowNode) {
      const id = queue.shift()!;
      if (visited.has(id)) continue;
      visited.add(id);
      for (const conn of connections) {
        if (conn.targetNodeId !== id || conn.kind !== "contains") continue;
        const parent = nodeById.get(conn.sourceNodeId);
        if (!parent) continue;
        if (parent.category === FLOW_NODES_KEY) {
          if (childFlowNodeIds.has(parent.id)) {
            flowNode = parent;
            break;
          }
          if (!walkFallback) walkFallback = parent;
        }
        queue.push(parent.id);
      }
    }
    if (!flowNode) flowNode = walkFallback;
    console.log("[SubtreePanel] Step 2 result: flowNode =", flowNode?.name ?? "null");
  }

  if (!flowNode) {
    console.log("[SubtreePanel] No flowNode found, returning empty");
    return { flowNode: null, subtreeNodes: [], subtreeConnections: [] };
  }

  // 3. Collect ALL descendant nodes of the selected flowNode so the panel
  //    always shows the complete subtree, not just the currently running slice.
  const subtreeNodes: CanvasNode[] = [flowNode];
  const subtreeNodeIds = new Set<string>([flowNode.id]);

  // BFS down via "contains" edges to gather every descendant.
  {
    const descQueue = [flowNode.id];
    while (descQueue.length > 0) {
      const did = descQueue.shift()!;
      for (const conn of connections) {
        if (conn.sourceNodeId !== did || conn.kind !== "contains") continue;
        if (subtreeNodeIds.has(conn.targetNodeId)) continue;
        const child = nodeById.get(conn.targetNodeId);
        if (!child || child.hidden) continue;
        subtreeNodes.push(child);
        subtreeNodeIds.add(child.id);
        descQueue.push(child.id);
      }
    }
  }

  // Also include nodes reachable via non-"contains" edges (Meets, etc.)
  // from the already-collected descendants.
  {
    const queue = [...subtreeNodeIds];
    for (const startId of queue) {
      for (const conn of connections) {
        if (conn.kind === "contains") continue;
        const neighborId =
          conn.sourceNodeId === startId ? conn.targetNodeId :
          conn.targetNodeId === startId ? conn.sourceNodeId : null;
        if (!neighborId || subtreeNodeIds.has(neighborId)) continue;
        const neighbor = nodeById.get(neighborId);
        if (!neighbor || neighbor.hidden || neighbor.category === FLOW_NODES_KEY) continue;
        subtreeNodes.push(neighbor);
        subtreeNodeIds.add(neighborId);
        queue.push(neighborId);
      }
    }
  }

  // Remove wrapper / renderAsSubtree nodes – keep them in the ID set so their
  // connections are preserved, but don't render them as visible nodes.
  const visibleSubtreeNodes = subtreeNodes.filter((n) => !n.renderAsSubtree);

  // 4. Collect all connections between the collected nodes.
  const subtreeConnections = connections.filter(
    (c) => subtreeNodeIds.has(c.sourceNodeId) && subtreeNodeIds.has(c.targetNodeId),
  );

  console.log("[SubtreePanel] Result: flowNode =", flowNode.name, "| visible nodes:", visibleSubtreeNodes.length, "| connections:", subtreeConnections.length);
  return { flowNode, subtreeNodes: visibleSubtreeNodes, subtreeConnections };
}

/* ── Panel component ── */
export default function SubtreePanel({
  nodes,
  connections,
  tickStatus,
  actionTypes,
  actionInstances,
}: SubtreePanelProps) {
  const [collapsed, setCollapsed] = useState(false);

  const current = useMemo(
    () => findActiveSubtree(nodes, connections, tickStatus),
    [nodes, connections, tickStatus],
  );

  // Keep the last non-empty result so the panel stays visible during brief gaps
  // between ticks (e.g. the pause at the end of a loop cycle).
  const lastValidRef = useRef(current);
  useEffect(() => {
    if (current.subtreeNodes.length > 0) lastValidRef.current = current;
  }, [current]);

  const { flowNode, subtreeNodes, subtreeConnections } =
    current.subtreeNodes.length > 0 ? current : lastValidRef.current;

  const hasContent = subtreeNodes.length > 0;

  // Re-fit the viewport in exactly two cases:
  //   1. The active FlowNode changes (new subtree layer).
  //   2. Action nodes first appear in the current subtree (length goes from 1
  //      to >1), so the view expands from "just the FlowNode" to the full layer.
  // After both triggers fire, we stop re-fitting until the next layer, which
  // avoids the hectic re-zoom on every individual action tick.
  const fitViewIds = useMemo(
    () => (flowNode ? subtreeNodes.map((n) => n.id) : undefined),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [`${flowNode?.id ?? ""}:${subtreeNodes.length > 1}`],
  );

  const title = flowNode
    ? flowNode.name || flowNode.typeLabel
    : "Active Subtree";

  return (
    <div className={`subtree-panel ${collapsed ? "subtree-panel--collapsed" : ""}`}>
      <div className="subtree-panel__header" onClick={() => setCollapsed((p: boolean) => !p)}>
        <span className="subtree-panel__title">{collapsed ? "Subtree" : title}</span>
        <button className="subtree-panel__toggle" title={collapsed ? "Expand" : "Collapse"}>
          {collapsed ? "◀" : "▶"}
        </button>
      </div>
      {!collapsed && (
        <div className="subtree-panel__body">
          {hasContent ? (
            <div className="subtree-panel__canvas">
              <EditorCanvas
                nodes={subtreeNodes}
                connections={subtreeConnections}
                tickStatus={tickStatus}
                actionTypes={actionTypes}
                actionInstances={actionInstances}
                fitViewNodeIds={fitViewIds}
                readOnly
                onDropNode={() => {}}
              />
            </div>
          ) : (
            <p className="subtree-panel__placeholder">No active execution</p>
          )}
        </div>
      )}
    </div>
  );
}
