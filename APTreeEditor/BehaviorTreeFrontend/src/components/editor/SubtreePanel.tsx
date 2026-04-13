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

  const nodeByName = new Map(nodes.filter((n) => n.name).map((n) => [n.name, n]));
  const nodeById   = new Map(nodes.map((n) => [n.id, n]));

  // 1. Try to find a running FlowNode directly in tickStatus – that gives us the
  //    most stable anchor (doesn't change on every action tick within the same layer).
  let flowNode: CanvasNode | null = null;
  for (const [name, status] of Object.entries(tickStatus)) {
    if (status !== "Running") continue;
    const n = nodeByName.get(name);
    if (n?.category === FLOW_NODES_KEY) { flowNode = n; break; }
  }

  // 2. If no running FlowNode is in tickStatus, find the running action node and
  //    walk up via "contains" connections to its parent FlowNode.
  if (!flowNode) {
    let actionNode: CanvasNode | undefined;
    for (const [name, status] of Object.entries(tickStatus)) {
      if (status !== "Running") continue;
      const n = nodeByName.get(name);
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

    // Walk "contains" edges upward until we hit a FlowNode
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
        if (parent.category === FLOW_NODES_KEY) { flowNode = parent; break; }
        queue.push(parent.id);
      }
    }
  }

  if (!flowNode) return { flowNode: null, subtreeNodes: [], subtreeConnections: [] };

  // 3. Collect action nodes via BFS from currently Running action nodes.
  //    Starting only from "Running" nodes (not stale "Success") avoids showing
  //    action nodes from previous layers that are still in tickStatus.
  const subtreeNodes: CanvasNode[] = [flowNode];
  const subtreeNodeIds = new Set<string>([flowNode.id]);

  const runningActionIds: string[] = [];
  for (const [name, status] of Object.entries(tickStatus)) {
    if (status !== "Running") continue;
    const n = nodeByName.get(name);
    if (!n || n.id === flowNode.id || n.category === FLOW_NODES_KEY || n.renderAsSubtree || n.hidden) continue;
    runningActionIds.push(n.id);
  }

  // BFS: expand to all nodes reachable via non-"contains" connections
  // (i.e. Meets / relation edges between sibling action nodes).
  const visited = new Set<string>(runningActionIds);
  const queue = [...runningActionIds];
  while (queue.length > 0) {
    const id = queue.shift()!;
    for (const conn of connections) {
      if (conn.kind === "contains") continue;
      const neighborId =
        conn.sourceNodeId === id ? conn.targetNodeId :
        conn.targetNodeId === id ? conn.sourceNodeId : null;
      if (!neighborId || visited.has(neighborId)) continue;
      const neighbor = nodeById.get(neighborId);
      if (!neighbor || neighbor.renderAsSubtree || neighbor.hidden || neighbor.category === FLOW_NODES_KEY) continue;
      visited.add(neighborId);
      queue.push(neighborId);
    }
  }

  for (const id of visited) {
    const n = nodeById.get(id);
    if (n) { subtreeNodes.push(n); subtreeNodeIds.add(id); }
  }

  // 4. Collect all connections between the collected nodes.
  const subtreeConnections = connections.filter(
    (c) => subtreeNodeIds.has(c.sourceNodeId) && subtreeNodeIds.has(c.targetNodeId),
  );

  return { flowNode, subtreeNodes, subtreeConnections };
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
