import { useState, useMemo } from "react";
import EditorCanvas from "./EditorCanvas";
import type { ActionInstance, ActionType } from "../sidebar/utils/types";
import type { SubtreeLayout } from "../../utils/aptreeImport";
import "./SubtreeViewer.css";

interface SubtreeViewerProps {
  subtreeLayouts: Map<string, SubtreeLayout>;
  actionTypes?: ActionType[];
  actionInstances?: ActionInstance[];
  tickStatus?: Record<string, string>;
}

/**
 * Collapsible right-panel that lets the user browse and inspect subtree graphs
 * from a multi-BehaviorTree import without loading them all into the main canvas.
 */
export default function SubtreeViewer({
  subtreeLayouts,
  actionTypes,
  actionInstances,
  tickStatus,
}: SubtreeViewerProps) {
  const [collapsed, setCollapsed] = useState(false);
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<string | null>(null);

  const names = useMemo(() => [...subtreeLayouts.keys()].sort(), [subtreeLayouts]);
  const filtered = useMemo(
    () =>
      search
        ? names.filter((n) => n.toLowerCase().includes(search.toLowerCase()))
        : names,
    [names, search],
  );

  // Nothing to show when there are no subtrees (single-graph import)
  if (subtreeLayouts.size === 0) return null;

  const layout = selected ? subtreeLayouts.get(selected) : null;
  const fitIds = layout?.nodes.map((n) => n.id);

  return (
    <div className={`subtree-viewer ${collapsed ? "subtree-viewer--collapsed" : ""}`}>
      <div
        className="subtree-viewer__header"
        onClick={() => setCollapsed((p) => !p)}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") setCollapsed((p) => !p);
        }}
      >
        <span className="subtree-viewer__title">
          {collapsed ? "Subtrees" : `Subtrees (${subtreeLayouts.size})`}
        </span>
        <button
          className="subtree-viewer__toggle"
          title={collapsed ? "Expand subtree browser" : "Collapse subtree browser"}
          onClick={(e) => { e.stopPropagation(); setCollapsed((p) => !p); }}
        >
          {collapsed ? "◀" : "▶"}
        </button>
      </div>

      {!collapsed && (
        <div className="subtree-viewer__body">
          <div className="subtree-viewer__list-area">
            <input
              className="subtree-viewer__search"
              type="search"
              placeholder="Search subtrees…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <div className="subtree-viewer__list">
              {filtered.map((name) => (
                <button
                  key={name}
                  className={`subtree-viewer__item${selected === name ? " subtree-viewer__item--active" : ""}`}
                  onClick={() => setSelected(name === selected ? null : name)}
                  title={name}
                >
                  {name}
                </button>
              ))}
              {filtered.length === 0 && (
                <p className="subtree-viewer__empty">No match</p>
              )}
            </div>
          </div>

          {layout && (
            <div className="subtree-viewer__canvas">
              <EditorCanvas
                key={selected}
                nodes={layout.nodes}
                connections={layout.connections}
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
      )}
    </div>
  );
}
