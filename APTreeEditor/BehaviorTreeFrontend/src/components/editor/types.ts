import type {
  ActionInstance,
  ActionType,
  DataCategory,
  FlowSuccessType,
} from "../sidebar/utils/types";
import type { DragEntityKind } from "./dragTypes";
import type { DraggedSidebarItem } from "./dragTypes";

/** serialized node information stored by the canvas component. */
export interface CanvasNode {
  id: string;
  sourceId: string;
  name: string;
  typeLabel: string;
  category: DataCategory;
  kind: DragEntityKind;
  x: number;
  y: number;
  width?: number;
  height?: number;
  isNegated?: boolean;
  successType?: FlowSuccessType;
  typeId?: string;
  preconditions?: PredicateInstance[];
  effects?: PredicateInstance[];
  /** Optional action arguments (e.g. beam1 fp2 r1) shown on the node card. */
  args?: string[];
  /** Render this node as a dashed subtree/container (used for NodeGraphs). */
  renderAsSubtree?: boolean;
  /** Optional title shown in the subtree container header. */
  subtreeTitle?: string;
  /** Optional service label merged into flow nodes (import only). */
  serviceLabel?: string;
  /** Hide node in the canvas (import-only layout helpers). */
  hidden?: boolean;
  /** True when the node has any outgoing connections (used for rendering ports/handles). */
  hasOutgoing?: boolean;
}

export const DEFAULT_CANVAS_NODE_WIDTH = 240;
export const DEFAULT_CANVAS_NODE_HEIGHT = 180;

/** represents a connection between two nodes. */
export interface NodeConnection {
  id: string;
  sourceNodeId: string;
  targetNodeId: string;
  sourcePort?: 'top' | 'right' | 'bottom' | 'left';
  targetPort?: 'top' | 'right' | 'bottom' | 'left';
  /** Connection kind from APTree export (e.g. child/service/member/relation). */
  kind?: string;
  /** Optional label from APTree export (e.g. Meets). */
  label?: string;
}

/** represents a visual separator line inside the canvas (used to split hierarchy levels). */
export interface CanvasSeparator {
  id: string;
  /** y-position in react-flow coordinates (top-left of the separator node). */
  y: number;
}

/** details surfaced when inspecting an action parameter from the canvas. */
export interface ActionParameterDetail {
  nodeId: string;
  nodeName: string;
  nodeTypeLabel: string;
  parameterId: string;
  parameterName: string;
  parameterType?: string;
  parameterValue?: string;
}

export type PredicateGroup = "precondition" | "effect";

export interface PredicateInstance {
  id: string;
  typeId: string;
  typeName: string;
  propertyValues: Record<string, string>;
  isNegated?: boolean;
}

/** contract for the editor canvas so the parent app can control interactions. */
export interface EditorCanvasProps {
  nodes: CanvasNode[];
  separators?: CanvasSeparator[];
  connections?: NodeConnection[];
  /** Canvas node id of the current root node (Flow node), or null when unset. */
  rootNodeId?: string | null;
  onDropNode: (
    item: DraggedSidebarItem,
    position: { x: number; y: number }
  ) => void;
  onBeginMoveNode?: (nodeId: string) => void;
  onBeginMoveSeparator?: (separatorId: string) => void;
  onDropSeparator?: (position: { x: number; y: number }) => void;
  onMoveNode?: (
    nodeId: string,
    position: { x: number; y: number }
  ) => void;
  onMoveSeparator?: (separatorId: string, y: number) => void;
  onResizeNode?: (
    nodeId: string,
    size: { width: number; height: number }
  ) => void;
  onRemoveNode?: (nodeId: string) => void;
  onRemoveSeparator?: (separatorId: string) => void;
  onEditNode?: (nodeId: string) => void;
  onAddConnection?: (
    sourceNodeId: string,
    targetNodeId: string,
    sourcePort: 'top' | 'right' | 'bottom' | 'left',
    targetPort: 'top' | 'right' | 'bottom' | 'left'
  ) => void;
  onRemoveConnection?: (connectionId: string) => void;
  onShowActionParameterDetail?: (detail: ActionParameterDetail) => void;
  onCycleFlowSuccessType?: (nodeId: string) => void;
  /** Sets the provided node id as the single Flow root node. */
  onSetRootNode?: (nodeId: string) => void;
  onOpenPredicateModal?: (nodeId: string, group: PredicateGroup) => void;
  actionTypes?: ActionType[];
  actionInstances?: ActionInstance[];
  /** Real-time tick status from the backend, keyed by node name. Values: "Running" | "Success" | "Failure" */
  tickStatus?: Record<string, string>;
}
