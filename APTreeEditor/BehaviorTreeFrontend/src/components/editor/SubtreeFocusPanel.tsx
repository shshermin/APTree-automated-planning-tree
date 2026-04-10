import EditorCanvas from "./EditorCanvas.tsx";
import type { CanvasNode, NodeConnection } from "./types";
import "./SubtreeFocusPanel.css";

interface SubtreeFocusPanelProps {
  subtreeName: string;
  nodes: CanvasNode[];
  connections: NodeConnection[];
  onClose: () => void;
}

export default function SubtreeFocusPanel({
  subtreeName,
  nodes,
  connections,
  onClose,
}: SubtreeFocusPanelProps) {
  return (
    <div className="subtree-focus-panel">
      <div className="subtree-focus-panel__header">
        <span className="subtree-focus-panel__title">{subtreeName}</span>
        <button
          className="subtree-focus-panel__close"
          onClick={onClose}
          aria-label="Close subtree panel"
        >
          ✕
        </button>
      </div>
      <div className="subtree-focus-panel__canvas">
        <EditorCanvas
          nodes={nodes}
          connections={connections}
          onDropNode={() => {}}
        />
      </div>
    </div>
  );
}
