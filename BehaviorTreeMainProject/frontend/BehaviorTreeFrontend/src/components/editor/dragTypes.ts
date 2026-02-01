import {
  ACTION_INSTANCES_KEY,
  ACTION_TYPES_KEY,
  BT_NODES_KEY,
  PARAM_TYPES_KEY,
  PREDICATE_TYPES_KEY,
} from "../sidebar/utils/constants";
import type { DataCategory } from "../sidebar/utils/types";

export const DRAG_DATA_FORMAT = "application/x-aptree-sidebar-item" as const;

export const CANVAS_TOOL_DRAG_DATA_FORMAT =
  "application/x-aptree-canvas-tool" as const;

// defines the various kinds of entities that can be dragged from the sidebar
export type DragEntityKind =
  | "parameterType"
  | "predicateType"
  | "actionType"
  | "actionInstance"
  | "behaviorNode"
  | "generic";

export type CanvasToolKind = "separatorLine";

export interface DraggedCanvasTool {
  tool: CanvasToolKind;
}

// defines the structure of the data payload when dragging an item from the sidebar
export interface DraggedSidebarItem {
  id: string;
  name: string;
  type: string;
  category: DataCategory;
  kind: DragEntityKind;
  typeId?: string;
  isNegated?: boolean;
}

/**
 * resolves the drag entity kind based on the originating sidebar category.
 * @param category category identifier associated with the dragged item
 * @returns matching drag entity kind value for the provided category
 */
export function resolveDragEntityKind(category: DataCategory): DragEntityKind {
  switch (category) {
    case PARAM_TYPES_KEY:
      return "parameterType";
    case PREDICATE_TYPES_KEY:
      return "predicateType";
    case ACTION_TYPES_KEY:
      return "actionType";
    case ACTION_INSTANCES_KEY:
      return "actionInstance";
    case BT_NODES_KEY:
      return "behaviorNode";
    default:
      return "generic";
  }
}

/**
 * checks whether the current drag operation originated from a sidebar item.
 * @param types list of mime types announced by the drag event
 * @returns true when the sidebar drag payload type is included
 */
export function isSidebarDrag(
  types: DOMStringList | readonly string[]
): boolean {
  if (Array.isArray(types)) {
    return types.includes(DRAG_DATA_FORMAT);
  }

  if ("contains" in types) {
    return (types as DOMStringList).contains(DRAG_DATA_FORMAT);
  }

  return false;
}

/**
 * checks whether the current drag operation originated from the canvas tools palette.
 * @param types list of mime types announced by the drag event
 * @returns true when the canvas tool drag payload type is included
 */
export function isCanvasToolDrag(
  types: DOMStringList | readonly string[]
): boolean {
  if (Array.isArray(types)) {
    return types.includes(CANVAS_TOOL_DRAG_DATA_FORMAT);
  }

  if ("contains" in types) {
    return (types as DOMStringList).contains(CANVAS_TOOL_DRAG_DATA_FORMAT);
  }

  return false;
}
