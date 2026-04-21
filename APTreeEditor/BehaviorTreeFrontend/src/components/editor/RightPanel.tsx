import { useState, useMemo, useEffect, useRef, useCallback } from "react";
import EditorCanvas from "./EditorCanvas";
import type { CanvasNode, NodeConnection } from "./types";
import type { ActionInstance, ActionType } from "../sidebar/utils/types";
import { FLOW_NODES_KEY } from "../sidebar/utils/constants";
import {
  computeSubtreeLayout,
  type RawSubtreeEntry,
  type SubtreeLayout,
} from "../../utils/aptreeImport";
import "./RightPanel.css";

// ── Tick diagnostics ──────────────────────────────────────────────────────
const TICK_DIAG = new URLSearchParams(window.location.search).has("tickDiag");
function diagLog(...args: unknown[]) {
  if (TICK_DIAG) console.log("[TickDiag]", ...args);
}

const ACTIVE_STATUSES = new Set(["Running", "InProgress"]);

// ── Live-execution types & helpers (ported from SubtreePanel) ─────────────
interface SubtreeResult {
  flowNode: CanvasNode;
  subtreeNodes: CanvasNode[];
  subtreeConnections: NodeConnection[];
}

function graphPrefix(id: string): string | null {
  const m = id.match(/^(bt-tree-\d+-)/);
  return m ? m[1] : null;
}

function collectSubtree(
  flowNode: CanvasNode,
  allNodes: CanvasNode[],
  nodeById: Map<string, CanvasNode>,
  connections: NodeConnection[],
): { subtreeNodes: CanvasNode[]; subtreeConnections: NodeConnection[] } {
  const subtreeNodeIds = new Set<string>([flowNode.id]);
  const prefix = graphPrefix(flowNode.id);

  if (prefix && !prefix.startsWith("bt-tree-0-")) {
    for (const node of allNodes) {
      if (!node.hidden && node.id.startsWith(prefix)) subtreeNodeIds.add(node.id);
    }
  } else {
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
        if (node.category === FLOW_NODES_KEY) continue;
        if (node.x >= oLeft && node.x <= oRight && node.y >= oTop && node.y <= oBottom) {
          subtreeNodeIds.add(node.id);
        }
      }
    }

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

  return {
    subtreeNodes: allNodes.filter((n) => subtreeNodeIds.has(n.id) && !n.renderAsSubtree),
    subtreeConnections: connections.filter(
      (c) => subtreeNodeIds.has(c.sourceNodeId) && subtreeNodeIds.has(c.targetNodeId),
    ),
  };
}

function findAllActiveSubtrees(
  nodes: CanvasNode[],
  connections: NodeConnection[],
  tickStatus: Record<string, string>,
): SubtreeResult[] {
  if (Object.keys(tickStatus).length === 0) return [];

  const nodeById = new Map(nodes.map((n) => [n.id, n]));
  const activeNodeIds = new Set<string>();
  for (const n of nodes) {
    if (!n.name) continue;
    const status = tickStatus[n.name];
    if (status && ACTIVE_STATUSES.has(status)) activeNodeIds.add(n.id);
  }
  if (activeNodeIds.size === 0) return [];

  const candidates: SubtreeResult[] = [];
  for (const fn of nodes.filter((n) => n.category === FLOW_NODES_KEY)) {
    if (!activeNodeIds.has(fn.id)) continue;
    candidates.push({ flowNode: fn, ...collectSubtree(fn, nodes, nodeById, connections) });
  }

  diagLog("Candidate subtrees:", candidates.map((c) => `${c.flowNode.name} (${c.subtreeNodes.length} nodes)`));

  const candidateIds = new Set(candidates.map((c) => c.flowNode.id));
  return candidates.filter((c) => {
    const containsOther = c.subtreeNodes.some(
      (n) => n.category === FLOW_NODES_KEY && n.id !== c.flowNode.id && candidateIds.has(n.id),
    );
    if (containsOther) diagLog(`Excluding ${c.flowNode.name}: contains other active FlowNodes`);
    return !containsOther;
  });
}

// ── Props ─────────────────────────────────────────────────────────────────
interface RightPanelProps {
  rawSubtreeGraphs: Map<string, RawSubtreeEntry>;
  nodes: CanvasNode[];
  connections: NodeConnection[];
  tickStatus: Record<string, string>;
  actionTypes?: ActionType[];
  actionInstances?: ActionInstance[];
}

type Tab = "subtrees" | "live";

// ── Component ─────────────────────────────────────────────────────────────
export default function RightPanel({
  rawSubtreeGraphs,
  nodes,
  connections,
  tickStatus,
  actionTypes,
  actionInstances,
}: RightPanelProps) {
  const [activeTab, setActiveTab] = useState<Tab>("subtrees");
  const [collapsed, setCollapsed] = useState(false);

  // ── Layout cache: computed on demand, cleared when import changes ──
  const layoutCache = useRef<Map<string, SubtreeLayout>>(new Map());
  useEffect(() => { layoutCache.current.clear(); }, [rawSubtreeGraphs]);

  const getLayout = useCallback((name: string): SubtreeLayout | undefined => {
    if (layoutCache.current.has(name)) return layoutCache.current.get(name);
    const entry = rawSubtreeGraphs.get(name);
    if (!entry) return undefined;
    const layout = computeSubtreeLayout(entry, actionTypes ?? []);
    layoutCache.current.set(name, layout);
    return layout;
  }, [rawSubtreeGraphs, actionTypes]);

  // ── Subtrees tab state ──
  const [search, setSearch] = useState("");
  const [selectedSubtree, setSelectedSubtree] = useState<string | null>(null);

  const subtreeNames = useMemo(() => [...rawSubtreeGraphs.keys()].sort(), [rawSubtreeGraphs]);
  const filteredNames = useMemo(
    () => (search ? subtreeNames.filter((n) => n.toLowerCase().includes(search.toLowerCase())) : subtreeNames),
    [subtreeNames, search],
  );
  const selectedLayout = selectedSubtree ? getLayout(selectedSubtree) : null;

  // ── Live tab: graph[0] subtrees (FlowNode level) ──
  const currentSubtrees = useMemo(
    () => findAllActiveSubtrees(nodes, connections, tickStatus),
    [nodes, connections, tickStatus],
  );
  const [lastValidSubtrees, setLastValidSubtrees] = useState(currentSubtrees);
  if (currentSubtrees.length > 0 && currentSubtrees !== lastValidSubtrees) {
    setLastValidSubtrees(currentSubtrees);
  }
  const liveSubtrees = currentSubtrees.length > 0 ? currentSubtrees : lastValidSubtrees;

  // ── Live tab: active subtree layouts (HL-action expansion level) ──
  // Cheap reverse-map built from raw node names — no layout computation needed.
  const nodeNameToLayoutName = useMemo(() => {
    const map = new Map<string, string>();
    for (const [layoutName, entry] of rawSubtreeGraphs) {
      for (const node of entry.graph.nodes) {
        if (node.name) map.set(node.name, layoutName);
      }
    }
    return map;
  }, [rawSubtreeGraphs]);

  const currentActiveLayouts = useMemo(() => {
    const active = new Set<string>();
    for (const [nodeName, status] of Object.entries(tickStatus)) {
      if (!ACTIVE_STATUSES.has(status)) continue;
      const layoutName = nodeNameToLayoutName.get(nodeName);
      if (layoutName) active.add(layoutName);
    }
    return [...active];
  }, [tickStatus, nodeNameToLayoutName]);

  const [lastValidLayouts, setLastValidLayouts] = useState(currentActiveLayouts);
  if (currentActiveLayouts.length > 0 && currentActiveLayouts !== lastValidLayouts) {
    setLastValidLayouts(currentActiveLayouts);
  }
  const liveLayoutNames = currentActiveLayouts.length > 0 ? currentActiveLayouts : lastValidLayouts;

  const hasActiveTicks = useMemo(
    () => Object.values(tickStatus).some((s) => ACTIVE_STATUSES.has(s)),
    [tickStatus],
  );

  const hasLiveContent = liveSubtrees.length > 0 || liveLayoutNames.length > 0;

  // Auto-switch to Live tab when execution starts
  const prevHasActive = useRef(false);
  useEffect(() => {
    if (hasActiveTicks && !prevHasActive.current) setActiveTab("live");
    prevHasActive.current = hasActiveTicks;
  }, [hasActiveTicks]);

  const showSubtreesTab = rawSubtreeGraphs.size > 0;

  // If there are no subtrees, default initial tab to "live"
  useEffect(() => {
    if (!showSubtreesTab) setActiveTab("live");
  }, [showSubtreesTab]);

  return (
    <div className={`right-panel ${collapsed ? "right-panel--collapsed" : ""}`}>
      {/* ── Header with tabs ── */}
      <div className="right-panel__header">
        {!collapsed && (
          <div className="right-panel__tabs">
            {showSubtreesTab && (
              <button
                className={`right-panel__tab${activeTab === "subtrees" ? " right-panel__tab--active" : ""}`}
                onClick={() => setActiveTab("subtrees")}
              >
                Subtrees
                <span className="right-panel__count">{rawSubtreeGraphs.size}</span>
              </button>
            )}
            <button
              className={`right-panel__tab${activeTab === "live" ? " right-panel__tab--active" : ""}`}
              onClick={() => setActiveTab("live")}
            >
              Live
              {hasActiveTicks && <span className="right-panel__badge" aria-label="active" />}
            </button>
          </div>
        )}
        {collapsed && <span className="right-panel__collapsed-label">Panel</span>}
        <button
          className="right-panel__collapse"
          onClick={() => setCollapsed((p) => !p)}
          title={collapsed ? "Expand panel" : "Collapse panel"}
        >
          ☰
        </button>
      </div>

      {/* ── Body ── */}
      {!collapsed && (
        <div className="right-panel__body">
          {/* Subtrees pane */}
          <div
            className="right-panel__pane"
            style={{ display: activeTab === "subtrees" ? "flex" : "none" }}
          >
            <div className="right-panel__list-area">
              <input
                className="right-panel__search"
                type="search"
                placeholder="Search subtrees…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
              <div className="right-panel__list">
                {filteredNames.map((name) => (
                  <button
                    key={name}
                    className={`right-panel__item${selectedSubtree === name ? " right-panel__item--active" : ""}`}
                    onClick={() => setSelectedSubtree(name === selectedSubtree ? null : name)}
                    title={name}
                  >
                    {name}
                  </button>
                ))}
                {filteredNames.length === 0 && (
                  <p className="right-panel__empty">No match</p>
                )}
              </div>
            </div>
            {selectedLayout ? (
              <div className="right-panel__canvas">
                <EditorCanvas
                  key={selectedSubtree}
                  nodes={selectedLayout.nodes}
                  connections={selectedLayout.connections}
                  tickStatus={tickStatus}
                  actionTypes={[...(actionTypes ?? []), ...selectedLayout.discoveredActionTypes]}
                  actionInstances={[...(actionInstances ?? []), ...selectedLayout.discoveredActionInstances]}
                  fitViewNodeIds={selectedLayout.nodes.map((n) => n.id)}
                  readOnly
                  onDropNode={() => {}}
                />
              </div>
            ) : (
              <p className="right-panel__placeholder">Select a subtree to inspect it</p>
            )}
          </div>

          {/* Live pane */}
          <div
            className="right-panel__pane"
            style={{ display: activeTab === "live" ? "flex" : "none" }}
          >
            {hasLiveContent ? (
              <>
                {liveSubtrees.map((sub) => (
                  <LiveSection
                    key={sub.flowNode.id}
                    subtree={sub}
                    tickStatus={tickStatus}
                    actionTypes={actionTypes}
                    actionInstances={actionInstances}
                  />
                ))}
                {liveLayoutNames.map((name) => {
                  const layout = getLayout(name);
                  if (!layout) return null;
                  return (
                    <LiveLayoutSection
                      key={name}
                      name={name}
                      layout={layout}
                      tickStatus={tickStatus}
                      actionTypes={[...(actionTypes ?? []), ...layout.discoveredActionTypes]}
                      actionInstances={[...(actionInstances ?? []), ...layout.discoveredActionInstances]}
                    />
                  );
                })}
              </>
            ) : (
              <p className="right-panel__placeholder">No active execution</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Live section for a subtree layout (HL-action expansion) ─────────────
function LiveLayoutSection({
  name,
  layout,
  tickStatus,
  actionTypes,
  actionInstances,
}: {
  name: string;
  layout: SubtreeLayout;
  tickStatus: Record<string, string>;
  actionTypes?: ActionType[];
  actionInstances?: ActionInstance[];
}) {
  const [collapsed, setCollapsed] = useState(false);
  const fitIds = useMemo(
    () => layout.nodes.map((n) => n.id),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [name],
  );

  return (
    <div className={`right-panel__section${collapsed ? " right-panel__section--collapsed" : ""}`}>
      <button
        className="right-panel__section-toggle"
        onClick={() => setCollapsed((p) => !p)}
        title={collapsed ? "Expand" : "Collapse"}
      >
        <span className={`right-panel__chevron${collapsed ? "" : " right-panel__chevron--open"}`}>&#9654;</span>
        <span className="right-panel__section-label" title={name}>{name}</span>
      </button>
      {!collapsed && (
        <div className="right-panel__canvas">
          <EditorCanvas
            nodes={layout.nodes}
            connections={layout.connections}
            tickStatus={tickStatus}
            actionTypes={actionTypes}
            actionInstances={actionInstances}
            fitViewNodeIds={fitIds}
            fitViewMaxZoom={0.6}
            readOnly
            onDropNode={() => {}}
          />
        </div>
      )}
    </div>
  );
}

// ── Live execution section (graph[0] FlowNode level) ─────────────────────
function LiveSection({
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
  const [collapsed, setCollapsed] = useState(true);
  const { flowNode, subtreeNodes, subtreeConnections } = subtree;
  const fitIds = useMemo(
    () => subtreeNodes.map((n) => n.id),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [`${flowNode.id}:${subtreeNodes.length > 1}`],
  );

  const compact = subtreeNodes.length <= 1;

  return (
    <div
      className={`right-panel__section${collapsed ? " right-panel__section--collapsed" : ""}`}
      style={!collapsed && compact ? { flex: 0, maxHeight: 200 } : undefined}
    >
      <button
        className="right-panel__section-toggle"
        onClick={() => setCollapsed((p) => !p)}
        title={collapsed ? "Expand" : "Collapse"}
      >
        <span className={`right-panel__chevron${collapsed ? "" : " right-panel__chevron--open"}`}>&#9654;</span>
        <span className="right-panel__section-label">
          {flowNode.name || flowNode.typeLabel}
        </span>
      </button>
      {!collapsed && (
        <div className="right-panel__canvas">
          <EditorCanvas
            nodes={subtreeNodes}
            connections={subtreeConnections}
            tickStatus={tickStatus}
            actionTypes={actionTypes}
            actionInstances={actionInstances}
            fitViewNodeIds={fitIds}
            readOnly
            onDropNode={() => {}}
          />
        </div>
      )}
    </div>
  );
}
