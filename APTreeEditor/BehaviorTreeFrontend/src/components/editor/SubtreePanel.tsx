import { useMemo, useState } from "react";
import EditorCanvas from "./EditorCanvas";
import type { CanvasNode, NodeConnection } from "./types";
import { ACTION_INSTANCES_KEY, FLOW_NODES_KEY } from "../sidebar/utils/constants";
import "./SubtreePanel.css";

/* ── Types ──────────────────────────────────────────────────────── */

interface SubtreePanelProps {
  nodes: CanvasNode[];
  connections: NodeConnection[];
  tickStatus: Record<string, string>;
}

/* ── Find active subtree ────────────────────────────────────────── */

/** Check if point (px,py) is inside a container node's bounding box. */
function isInsideContainer(px: number, py: number, container: CanvasNode): boolean {
  const w = container.width ?? 0;
  const h = container.height ?? 0;
  // Container x/y is center, so bounds are x ± w/2, y ± h/2
  const left = container.x - w / 2;
  const right = container.x + w / 2;
  const top = container.y - h / 2;
  const bottom = container.y + h / 2;
  return px >= left && px <= right && py >= top && py <= bottom;
}

function findActiveSubtree(
  nodes: CanvasNode[],
  connections: NodeConnection[],
  tickStatus: Record<string, string>,
): { flowNode: CanvasNode | null; subtreeNodes: CanvasNode[]; subtreeConnections: NodeConnection[] } {
  // 1. Find running or successful node names
  const runningNames = new Set(
    Object.entries(tickStatus)
      .filter(([, s]) => s === "Running")
      .map(([name]) => name),
  );
  const targetNames = runningNames.size > 0
    ? runningNames
    : new Set(
        Object.entries(tickStatus)
          .filter(([, s]) => s === "Success")
          .map(([name]) => name),
      );
  if (targetNames.size === 0) return { flowNode: null, subtreeNodes: [], subtreeConnections: [] };

  const nodeByName = new Map(nodes.filter((n) => n.name).map((n) => [n.name, n]));
  const nodeById = new Map(nodes.map((n) => [n.id, n]));

  // 2. Find the active action node
  let targetNode: CanvasNode | undefined;
  for (const name of targetNames) {
    const node = nodeByName.get(name);
    if (node && (node.category === ACTION_INSTANCES_KEY || node.kind === "actionInstance")) {
      targetNode = node;
      break;
    }
  }
  if (!targetNode) {
    for (const name of targetNames) {
      const node = nodeByName.get(name);
      if (node) { targetNode = node; break; }
    }
  }
  if (!targetNode) return { flowNode: null, subtreeNodes: [], subtreeConnections: [] };

  // 3. Find the subtree wrapper (renderAsSubtree=true) that spatially contains this action.
  //    Pick the smallest wrapper that contains the action (most specific).
  const wrappers = nodes.filter((n) => n.renderAsSubtree && n.width && n.height);
  let bestWrapper: CanvasNode | null = null;
  let bestArea = Infinity;
  for (const wrapper of wrappers) {
    if (isInsideContainer(targetNode.x, targetNode.y, wrapper)) {
      const area = (wrapper.width ?? 0) * (wrapper.height ?? 0);
      if (area < bestArea) {
        bestArea = area;
        bestWrapper = wrapper;
      }
    }
  }

  // 4. Find the FlowNode that has a "contains" connection to this wrapper (or its parent wrapper).
  let flowNode: CanvasNode | null = null;
  if (bestWrapper) {
    // Walk contains-connections upward from wrapper to find FlowNode
    const visited = new Set<string>();
    const queue = [bestWrapper.id];
    while (queue.length > 0) {
      const currentId = queue.shift()!;
      if (visited.has(currentId)) continue;
      visited.add(currentId);
      // Find "contains" connections where this node is the target (i.e., parent → this)
      for (const conn of connections) {
        if (conn.targetNodeId === currentId && conn.kind === "contains") {
          const parent = nodeById.get(conn.sourceNodeId);
          if (parent?.category === FLOW_NODES_KEY) {
            flowNode = parent;
            break;
          }
          queue.push(conn.sourceNodeId);
        }
      }
      if (flowNode) break;
    }
  }

  // 5. If we found a FlowNode, find its subtree wrapper (the nodegraph-wrapper it connects to),
  //    then collect all action nodes spatially inside that wrapper.
  let subtreeWrapper = bestWrapper;
  if (flowNode) {
    // The FlowNode's direct "contains" target is the nodegraph wrapper
    for (const conn of connections) {
      if (conn.sourceNodeId === flowNode.id && conn.kind === "contains") {
        const target = nodeById.get(conn.targetNodeId);
        if (target?.renderAsSubtree) {
          subtreeWrapper = target;
          break;
        }
      }
    }
  }

  if (!subtreeWrapper) {
    return { flowNode: null, subtreeNodes: [targetNode], subtreeConnections: [] };
  }

  // 6. Collect all visible nodes inside this wrapper
  const subtreeNodes: CanvasNode[] = [];
  const subtreeNodeIds = new Set<string>();

  // Include the FlowNode itself
  if (flowNode) {
    subtreeNodes.push(flowNode);
    subtreeNodeIds.add(flowNode.id);
  }

  // Include all non-hidden, non-wrapper nodes spatially inside the wrapper
  for (const n of nodes) {
    if (n.renderAsSubtree || n.hidden) continue;
    if (n.id === flowNode?.id) continue; // already added
    if (isInsideContainer(n.x, n.y, subtreeWrapper)) {
      subtreeNodes.push(n);
      subtreeNodeIds.add(n.id);
    }
  }

  // 7. Collect connections between these nodes (Meets relations + any others)
  const subtreeConnections = connections.filter(
    (c) => subtreeNodeIds.has(c.sourceNodeId) && subtreeNodeIds.has(c.targetNodeId),
  );

  return { flowNode, subtreeNodes, subtreeConnections };
}

/* ── Panel component ────────────────────────────────────────────── */

export default function SubtreePanel({ nodes, connections, tickStatus }: SubtreePanelProps) {
  const [collapsed, setCollapsed] = useState(false);

  const { flowNode, subtreeNodes, subtreeConnections } = useMemo(
    () => findActiveSubtree(nodes, connections, tickStatus),
    [nodes, connections, tickStatus],
  );

  const hasContent = subtreeNodes.length > 0;

  const title = flowNode
    ? flowNode.name || flowNode.typeLabel
    : "Active Subtree";

  return (
    <div className={`subtree-panel ${collapsed ? "subtree-panel--collapsed" : ""}`}>
      <div className="subtree-panel__header" onClick={() => setCollapsed((p) => !p)}>
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
