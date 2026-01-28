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

/** contract for the editor canvas so the parent app can control interactions. */
export interface EditorCanvasProps {
  nodes: CanvasNode[];
  connections?: NodeConnection[];
  onDropNode: (
    item: DraggedSidebarItem,
    position: { x: number; y: number }
  ) => void;
  onMoveNode?: (
    nodeId: string,
    position: { x: number; y: number }
  ) => void;
  onResizeNode?: (
    nodeId: string,
    size: { width: number; height: number }
  ) => void;
  onRemoveNode?: (nodeId: string) => void;
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
  actionTypes?: ActionType[];
  actionInstances?: ActionInstance[];
}
