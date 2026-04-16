import { useMemo, useState } from "react";
import EditorCanvas from "./EditorCanvas";
import type { CanvasNode, NodeConnection } from "./types";
import type { ActionInstance, ActionType } from "../sidebar/utils/types";
import { FLOW_NODES_KEY } from "../sidebar/utils/constants";
import "./SubtreePanel.css";

/* ── Diagnostic logging ── */
const TICK_DIAG = new URLSearchParams(window.location.search).has("tickDiag");

/** Statuses that indicate a node is currently active. */
const ACTIVE_STATUSES = new Set(["Running", "InProgress"]);

function diagLog(...args: unknown[]) {
  if (TICK_DIAG) console.log("[TickDiag]", ...args);
}

/* ── Types ── */
interface SubtreePanelProps {
  nodes: CanvasNode[];
  connections: NodeConnection[];
  tickStatus: Record<string, string>;
  actionTypes?: ActionType[];
  actionInstances?: ActionInstance[];
}

interface SubtreeResult {
  flowNode: CanvasNode;
  subtreeNodes: CanvasNode[];
  subtreeConnections: NodeConnection[];
}

/** Extract the graph-origin prefix from a node ID, e.g. "bt-tree-5-" */
function graphPrefix(id: string): string | null {
  const m = id.match(/^(bt-tree-\d+-)/);
  return m ? m[1] : null;
}

/**
 * Collect the subtree for a FlowNode.
 *
 * For FlowNodes from the main tree (bt-tree-0-):
 *   Follow "contains" edges to find wrapper nodes, then use geometric
 *   containment to find action nodes inside the wrappers.
 *
 * For FlowNodes from subtree graphs (bt-tree-N-, N>0):
 *   Simply collect ALL nodes sharing the same prefix — the entire subtree
 *   graph belongs to this FlowNode.
 */
function collectSubtree(
  flowNode: CanvasNode,
  allNodes: CanvasNode[],
  nodeById: Map<string, CanvasNode>,
  connections: NodeConnection[],
): { subtreeNodes: CanvasNode[]; subtreeConnections: NodeConnection[] } {
  const subtreeNodeIds = new Set<string>([flowNode.id]);
  const prefix = graphPrefix(flowNode.id);

  // For subtree graphs (index > 0): simply collect all nodes with the same prefix
  if (prefix && !prefix.startsWith("bt-tree-0-")) {
    for (const node of allNodes) {
      if (!node.hidden && node.id.startsWith(prefix)) {
        subtreeNodeIds.add(node.id);
      }
    }
  } else {
    // Main tree: use geometric containment via wrappers

    // Step 1: BFS via "contains" edges to find wrapper nodes
    const wrappers: CanvasNode[] = [];
    {
      const queue = [flowNode.id];
      while (queue.length > 0) {
        const did = queue.shift()!;
        for (const conn of connections) {
          if (conn.sourceNodeId !== did || conn.kind !== "contains") continue;
          if (subtreeNodeIds.has(conn.targetNodeId)) continue;
          const child = nodeById.get(conn.targetNodeId);
          if (!child || child.hidden) continue;
          subtreeNodeIds.add(child.id);
          if (child.renderAsSubtree) wrappers.push(child);
          queue.push(child.id);
        }
      }
    }

    // Step 2: Geometric containment — find nodes inside wrappers.
    // Skip the "bt-outer-subtree" wrapper (it encompasses the entire tree).
    for (const wrapper of wrappers) {
      if (wrapper.id.includes("bt-outer-subtree")) continue;
      const ow = wrapper.width ?? 0;
      const oh = wrapper.height ?? 0;
      const oLeft = wrapper.x - ow / 2;
      const oRight = wrapper.x + ow / 2;
      const oTop = wrapper.y - oh / 2;
      const oBottom = wrapper.y + oh / 2;
      for (const node of allNodes) {
        if (subtreeNodeIds.has(node.id) || node.hidden) continue;
        if (node.category === FLOW_NODES_KEY) continue; // don't pull in other FlowNodes
        if (node.x >= oLeft && node.x <= oRight && node.y >= oTop && node.y <= oBottom) {
          subtreeNodeIds.add(node.id);
        }
      }
    }

    // Step 3: Follow non-"contains" edges (Meets, etc.)
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
          subtreeNodeIds.add(neighborId);
          queue.push(neighborId);
        }
      }
    }
  }

  const subtreeNodes = allNodes.filter(
    (n) => subtreeNodeIds.has(n.id) && !n.renderAsSubtree,
  );
  const subtreeConnections = connections.filter(
    (c) => subtreeNodeIds.has(c.sourceNodeId) && subtreeNodeIds.has(c.targetNodeId),
  );

  return { subtreeNodes, subtreeConnections };
}

function findAllActiveSubtrees(
  nodes: CanvasNode[],
  connections: NodeConnection[],
  tickStatus: Record<string, string>,
): SubtreeResult[] {
  if (Object.keys(tickStatus).length === 0) return [];

  const nodeById = new Map(nodes.map((n) => [n.id, n]));

  // Find ALL active canvas nodes
  const activeNodeIds = new Set<string>();
  for (const n of nodes) {
    if (!n.name) continue;
    const status = tickStatus[n.name];
    if (status && ACTIVE_STATUSES.has(status)) {
      activeNodeIds.add(n.id);
    }
  }

  if (activeNodeIds.size === 0) return [];

  // Get all FlowNodes
  const allFlowNodes = nodes.filter((n) => n.category === FLOW_NODES_KEY);

  // Build subtree for each FlowNode and check if it has active content
  const candidates: SubtreeResult[] = [];
  for (const fn of allFlowNodes) {
    // Skip the FlowNode if it's not active itself (optimization)
    if (!activeNodeIds.has(fn.id)) continue;

    const sub = collectSubtree(fn, nodes, nodeById, connections);
    candidates.push({ flowNode: fn, ...sub });
  }

  diagLog("Candidate subtrees:", candidates.map((c) =>
    `${c.flowNode.name} (${c.subtreeNodes.length} nodes)`));

  // Filter out "parent" subtrees that contain other candidate FlowNodes.
  // e.g. "Main" contains Layers1_2, so Main should be excluded.
  const candidateFlowNodeIds = new Set(candidates.map((c) => c.flowNode.id));
  const results = candidates.filter((c) => {
    const containsOtherFlowNode = c.subtreeNodes.some(
      (n) => n.category === FLOW_NODES_KEY && n.id !== c.flowNode.id && candidateFlowNodeIds.has(n.id),
    );
    if (containsOtherFlowNode) {
      diagLog(`Excluding ${c.flowNode.name}: contains other active FlowNodes`);
    }
    return !containsOtherFlowNode;
  });

  diagLog("Final subtrees:", results.map((r) => r.flowNode.name));
  return results;
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

  const currentSubtrees = useMemo(
    () => findAllActiveSubtrees(nodes, connections, tickStatus),
    [nodes, connections, tickStatus],
  );

  // Keep the last non-empty result so the panel stays visible during brief gaps
  // between ticks (e.g. the pause at the end of a loop cycle).
  const [lastValid, setLastValid] = useState(currentSubtrees);
  if (currentSubtrees.length > 0 && currentSubtrees !== lastValid) {
    setLastValid(currentSubtrees);
  }

  const subtrees = currentSubtrees.length > 0 ? currentSubtrees : lastValid;
  const hasContent = subtrees.length > 0;

  const title = hasContent
    ? subtrees.map((s) => s.flowNode.name || s.flowNode.typeLabel).join(" + ")
    : "Active Subtrees";

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
            subtrees.map((sub) => (
              <SubtreeSection
                key={sub.flowNode.id}
                subtree={sub}
                tickStatus={tickStatus}
                actionTypes={actionTypes}
                actionInstances={actionInstances}
              />
            ))
          ) : (
            <p className="subtree-panel__placeholder">No active execution</p>
          )}
        </div>
      )}
    </div>
  );
}

/* ── Individual subtree section ── */
function SubtreeSection({
  subtree,
  tickStatus,
  actionTypes,
  actionInstances,
}: {
  subtree: SubtreeResult;
  tickStatus: Record<string, string>;
  actionTypes?: ActionType[];
  actionInstances?: ActionInstance[];
}) {
  const { flowNode, subtreeNodes, subtreeConnections } = subtree;

  const fitViewIds = useMemo(
    () => subtreeNodes.map((n) => n.id),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [`${flowNode.id}:${subtreeNodes.length > 1}`],
  );

  return (
    <div className="subtree-panel__section">
      <div className="subtree-panel__section-label">
        {flowNode.name || flowNode.typeLabel}
      </div>
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
    </div>
  );
}
