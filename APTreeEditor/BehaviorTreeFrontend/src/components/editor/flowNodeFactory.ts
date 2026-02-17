import type { CanvasNode } from "./types";
import { DEFAULT_CANVAS_NODE_HEIGHT, DEFAULT_CANVAS_NODE_WIDTH } from "./types";
import type { BehaviorNodeOption } from "../sidebar/utils/types";
import { FLOW_SUCCESS_TYPES } from "../sidebar/utils/types";
import { createId } from "../../utils/id";
import {
  BT_NODES_KEY,
  DECORATOR_NODES_KEY,
  FLOW_NODES_KEY,
  SERVICE_NODES_KEY,
} from "../sidebar/utils/constants";

/** maps sidebar option kind to the target sidebar category stored on the node. */
const categoryByKind = {
  flow: FLOW_NODES_KEY,
  decorator: DECORATOR_NODES_KEY,
  service: SERVICE_NODES_KEY,
  nodeGraph: BT_NODES_KEY,
} as const;

/** defines input data required to instantiate a behavior node on the canvas. */
interface BehaviorNodeFactoryParams {
  option: BehaviorNodeOption;
  position?: { x: number; y: number };
}

/**
 * creates a default canvas node representation for a chosen behavior node option.
 */
export function createBehaviorNode({
  option,
  position = { x: 120, y: 120 },
}: BehaviorNodeFactoryParams): CanvasNode {
  const category = categoryByKind[option.kind];
  const successType =
    option.kind === "flow"
      ? option.defaultSuccessType ?? FLOW_SUCCESS_TYPES[0]
      : undefined;

  return {
    id: createId("bt-node"),
    sourceId: option.id,
    name: option.label,
    typeLabel: option.typeLabel,
    category,
    kind: "behaviorNode",
    x: position.x,
    y: position.y,
    width: DEFAULT_CANVAS_NODE_WIDTH,
    height: DEFAULT_CANVAS_NODE_HEIGHT,
    successType,
  };
}
