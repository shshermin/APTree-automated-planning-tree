import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { CSSProperties } from "react";
import {
  Background,
  BackgroundVariant,
  BaseEdge,
  EdgeLabelRenderer,
  getSmoothStepPath,
  Handle,
  MarkerType,
  NodeResizer,
  type Connection,
  ConnectionLineType,
  type Edge as FlowEdge,
  type EdgeProps,
  type EdgeTypes,
  ReactFlow,
  ReactFlowProvider,
  type Node as FlowNode,
  type NodeProps,
  type NodeTypes,
  Position,
  useReactFlow,
  useStore,
  useUpdateNodeInternals,
} from "reactflow";
import "reactflow/dist/style.css";
import {
  CANVAS_TOOL_DRAG_DATA_FORMAT,
  DRAG_DATA_FORMAT,
  isCanvasToolDrag,
  isSidebarDrag,
  type DraggedCanvasTool,
  type DraggedSidebarItem,
} from "./dragTypes";
import type {
  ActionParameterDetail,
  CanvasNode,
  EditorCanvasProps,
  PredicateGroup,
} from "./types";
import {
  DEFAULT_CANVAS_NODE_HEIGHT,
  DEFAULT_CANVAS_NODE_WIDTH,
} from "./types";
import type {
  ActionInstance,
  ActionType,
} from "../sidebar/utils/types";
import {
  DECORATOR_NODES_KEY,
  FLOW_NODES_KEY,
  SERVICE_NODES_KEY,
} from "../sidebar/utils/constants";
import "./EditorCanvas.css";

type PortSide = "top" | "right" | "bottom" | "left";

const resolveNumericOffset = (value: string | number | undefined): number => {
  if (typeof value === "number") {
    return value;
  }

  if (typeof value === "string") {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  return 0;
};

interface BehaviorNodeData {
  node: CanvasNode;
  rootNodeId?: string | null;
  actionTypeMap: Map<string, ActionType>;
  actionInstanceMap: Map<string, ActionInstance>;
  onRemoveNode?: (nodeId: string) => void;
  onEditNode?: (nodeId: string) => void;
  onCycleFlowSuccessType?: (nodeId: string) => void;
  onSetRootNode?: (nodeId: string) => void;
  onResizeNode?: (nodeId: string, size: { width: number; height: number }) => void;
  onMoveNode?: (nodeId: string, position: { x: number; y: number }) => void;
  onShowActionParameterDetail?: (detail: ActionParameterDetail) => void;
  onOpenPredicateModal?: (nodeId: string, group: PredicateGroup) => void;
  /** Real-time tick status: "Running" | "Success" | "Failure" | undefined */
  tickStatus?: string;
}

interface BehaviorEdgeData {
  onRemoveConnection?: (connectionId: string) => void;
  isHovered?: boolean;
  kind?: string;
  label?: string;
}

interface SeparatorNodeData {
  label: string;
  onRemoveSeparator?: (separatorId: string) => void;
  isHovered?: boolean;
}

interface SubtreeNodeData {
  node: CanvasNode;
}

const portPositions: Record<PortSide, Position> = {
  top: Position.Top,
  right: Position.Right,
  bottom: Position.Bottom,
  left: Position.Left,
};

const portPoint = (node: CanvasNode, port: PortSide) => {
  const halfW = (node.width ?? DEFAULT_CANVAS_NODE_WIDTH) / 2;
  const halfH = (node.height ?? DEFAULT_CANVAS_NODE_HEIGHT) / 2;
  switch (port) {
    case "left":
      return { x: node.x - halfW, y: node.y };
    case "right":
      return { x: node.x + halfW, y: node.y };
    case "top":
      return { x: node.x, y: node.y - halfH };
    case "bottom":
      return { x: node.x, y: node.y + halfH };
  }
};

const chooseShortestPorts = (
  source: CanvasNode,
  target: CanvasNode,
  sourcePorts: PortSide[],
  targetPorts: PortSide[]
) => {
  let best: { sourcePort: PortSide; targetPort: PortSide; d2: number } | null = null;
  for (const sp of sourcePorts) {
    const a = portPoint(source, sp);
    for (const tp of targetPorts) {
      const b = portPoint(target, tp);
      const dx = a.x - b.x;
      const dy = a.y - b.y;
      const d2 = dx * dx + dy * dy;
      if (!best || d2 < best.d2) {
        best = { sourcePort: sp, targetPort: tp, d2 };
      }
    }
  }
  return best ?? { sourcePort: "bottom", targetPort: "top", d2: 0 };
};

const PORT_STYLES: Record<PortSide, CSSProperties> = {
  top: { top: -6, left: "50%", transform: "translate(-50%, 0)" },
  right: { right: -6, top: "50%", transform: "translate(0, -50%)" },
  bottom: { bottom: -6, left: "50%", transform: "translate(-50%, 0)" },
  left: { left: -6, top: "50%", transform: "translate(0, -50%)" },
};

const SOURCE_HANDLE_STYLES: Record<PortSide, CSSProperties> = {
  top: { top: -6, left: "50%", transform: "translate(-50%, 0)" },
  right: { right: -6, top: "50%", transform: "translate(0, -50%)" },
  bottom: { bottom: -6, left: "50%", transform: "translate(-50%, 0)" },
  left: { left: -6, top: "50%", transform: "translate(0, -50%)" },
};

const TARGET_HANDLE_STYLES: Record<PortSide, CSSProperties> = {
  top: { top: -6, left: "50%", transform: "translate(-50%, 0)" },
  right: { right: -6, top: "50%", transform: "translate(0, -50%)" },
  bottom: { bottom: -6, left: "50%", transform: "translate(-50%, 0)" },
  left: { left: -6, top: "50%", transform: "translate(0, -50%)" },
};

const CANVAS_EXTENT: [[number, number], [number, number]] = [
  [-200000, -200000],
  [200000, 200000],
];

const PARAM_BOX_HEIGHT = 24;
const PARAM_BOX_GAP = 6;
const PARAM_STACK_CLEARANCE = 18;
const SUCCESS_BADGE_CLEARANCE = 32;

/**
 * checks if a canvas node is an action node.
 * @param node the canvas node to check 
 * @returns true if the node is an action node, false otherwise 
 */
function isActionNode(node: CanvasNode) {
  return node.kind === "actionType" || node.kind === "actionInstance";
}

/**
 * behavior tree node component for the editor canvas.
 * @param param0 component props 
 * @returns JSX element 
 */
function BehaviorTreeNode({ id, data, selected }: NodeProps<BehaviorNodeData>) {
  const updateNodeInternals = useUpdateNodeInternals();
  const { node } = data;

  const isFlowNode = node.category === FLOW_NODES_KEY;
  const isMergedFlow = isFlowNode && !!node.serviceLabel;

  const shouldRenderSourceHandles =
    isFlowNode ||
    isActionNode(node) ||
    !!node.hasOutgoing;

  const isAction = isActionNode(node);
  const actionInstance =
    isAction && node.kind === "actionInstance"
      ? data.actionInstanceMap.get(node.sourceId)
      : undefined;

  const resolvedActionTypeId = isAction
    ? node.kind === "actionType"
      ? node.sourceId
      : actionInstance?.typeId ?? node.typeId
    : undefined;

  const actionTypeDefinition =
    resolvedActionTypeId && isAction
      ? data.actionTypeMap.get(resolvedActionTypeId)
      : undefined;

  type ActionParameterSummary = {
    id: string;
    propertyId: string;
    name: string;
    value?: string;
    valueType?: string;
  };

  const actionParameterSummaries: ActionParameterSummary[] =
    isAction && actionTypeDefinition
      ? actionTypeDefinition.properties.reduce<ActionParameterSummary[]>((list, property, index) => {
          const fallbackId = `${resolvedActionTypeId ?? "action"}-${index}`;
          const propertyId = property.id?.trim() || fallbackId;
          const name = property.name?.trim() || property.id || `param_${index + 1}`;
          if (!name.trim()) {
            return list;
          }

          list.push({
            id: `${node.id}-${propertyId}`,
            propertyId,
            name,
            value: property.id ? actionInstance?.propertyValues?.[property.id] : undefined,
            valueType: property.valueType,
          });

          return list;
        }, [])
      : [];

  const showActionParams = actionParameterSummaries.length > 0;
  const paramStackHeight = showActionParams
    ? actionParameterSummaries.length * PARAM_BOX_HEIGHT +
      (actionParameterSummaries.length - 1) * PARAM_BOX_GAP
    : 0;
  const paramClearance = showActionParams
    ? paramStackHeight + PARAM_STACK_CLEARANCE
    : 0;

  useEffect(() => {
    updateNodeInternals(id);
  }, [id, paramClearance, showActionParams, updateNodeInternals]);

  const nodeClasses = ["canvas-node", `canvas-node-${node.kind}`];

  const isRootNode = !!data.rootNodeId && data.rootNodeId === id;
  if (isRootNode) {
    nodeClasses.push("canvas-node-rooted");
  }

  if (data.tickStatus === "Running") {
    nodeClasses.push("canvas-node-tick-running");
  } else if (data.tickStatus === "Success") {
    nodeClasses.push("canvas-node-tick-success");
  } else if (data.tickStatus === "Failure") {
    nodeClasses.push("canvas-node-tick-failure");
  }
  const shouldRenderActions =
    !!data.onEditNode || !!data.onRemoveNode;
  const shouldRenderActionRow = !!data.onEditNode || !!data.onRemoveNode;

  if (node.typeLabel === "NodeGraph") {
    nodeClasses.push("canvas-node-nodegraph");
  }

  if (node.category === FLOW_NODES_KEY) {
    nodeClasses.push("canvas-node-flow");
    if (isMergedFlow) {
      nodeClasses.push("canvas-node-flow-merged");
    }
  } else if (node.category === DECORATOR_NODES_KEY) {
    nodeClasses.push("canvas-node-decorator");
  } else if (node.category === SERVICE_NODES_KEY) {
    nodeClasses.push("canvas-node-service");
  }

  if (isAction) {
    nodeClasses.push("canvas-node-action");
  }

  const portStyleOverrides: Partial<Record<PortSide, CSSProperties>> = {};
  const sourceHandleOverrides: Partial<Record<PortSide, CSSProperties>> = {};
  const targetHandleOverrides: Partial<Record<PortSide, CSSProperties>> = {};

  if (node.category === FLOW_NODES_KEY) {
    const offset = "-2%";

    portStyleOverrides.left = { left: offset };
    portStyleOverrides.right = { right: offset };

    sourceHandleOverrides.left = { left: offset };
    sourceHandleOverrides.right = { right: offset };

    targetHandleOverrides.left = { left: offset };
    targetHandleOverrides.right = { right: offset };
  }

  const successBadgeClearance = node.successType ? SUCCESS_BADGE_CLEARANCE : 0;
  const topClearance = isFlowNode ? 0 : successBadgeClearance;

  if (topClearance > 0) {
    const basePortTop = resolveNumericOffset(PORT_STYLES.top.top);
    const baseSourceTop = resolveNumericOffset(SOURCE_HANDLE_STYLES.top.top);
    const baseTargetTop = resolveNumericOffset(TARGET_HANDLE_STYLES.top.top);

    portStyleOverrides.top = {
      ...(portStyleOverrides.top ?? {}),
      top: basePortTop - topClearance,
    };
    sourceHandleOverrides.top = {
      ...(sourceHandleOverrides.top ?? {}),
      top: baseSourceTop - topClearance,
    };
    targetHandleOverrides.top = {
      ...(targetHandleOverrides.top ?? {}),
      top: baseTargetTop - topClearance,
    };
  }

  return (
    <div className={nodeClasses.join(" ")}>
      <NodeResizer
        isVisible={selected}
        minWidth={180}
        minHeight={120}
        onResizeEnd={(_event, params) => {
          if (!data.onResizeNode && !data.onMoveNode) {
            return;
          }

          const previousWidth = data.node.width ?? DEFAULT_CANVAS_NODE_WIDTH;
          const previousHeight = data.node.height ?? DEFAULT_CANVAS_NODE_HEIGHT;
          const nextWidth = params.width ?? previousWidth;
          const nextHeight = params.height ?? previousHeight;

          const previousTopLeft = {
            x: data.node.x - previousWidth / 2,
            y: data.node.y - previousHeight / 2,
          };

          const nextTopLeft = {
            x: params.x ?? previousTopLeft.x,
            y: params.y ?? previousTopLeft.y,
          };

          data.onResizeNode?.(id, {
            width: nextWidth,
            height: nextHeight,
          });

          data.onMoveNode?.(id, {
            x: nextTopLeft.x + nextWidth / 2,
            y: nextTopLeft.y + nextHeight / 2,
          });
        }}
        handleClassName="canvas-node-resizer-handle"
        lineClassName="canvas-node-resizer-line"
      />

      {node.successType && !isFlowNode ? (
        <div className="canvas-node-success-row nodrag">
          {data.onCycleFlowSuccessType ? (
            <button
              type="button"
              className="canvas-node-success nodrag"
              onMouseDown={(event) => event.stopPropagation()}
              onClick={(event) => {
                event.stopPropagation();
                data.onCycleFlowSuccessType?.(id);
              }}
              title="Click to cycle success type"
              aria-label={`Success type ${node.successType}. Click to cycle.`}
            >
              {node.successType}
            </button>
          ) : (
            <span className="canvas-node-success" aria-hidden="true">
              {node.successType}
            </span>
          )}
        </div>
      ) : null}

      {shouldRenderActions ? (
        <div className="canvas-node-actions nodrag" aria-label="Node actions">
          {shouldRenderActionRow ? (
            <div className="canvas-node-actions-row">
              {data.onEditNode ? (
                <button
                  type="button"
                  className="canvas-node-edit nodrag"
                  onMouseDown={(event) => event.stopPropagation()}
                  onClick={(event) => {
                    event.stopPropagation();
                    data.onEditNode?.(id);
                  }}
                  aria-label={`Edit ${node.name}`}
                  title="Edit"
                >
                  ✎
                </button>
              ) : null}

              {data.onRemoveNode ? (
                <button
                  type="button"
                  className="canvas-node-remove nodrag"
                  onMouseDown={(event) => event.stopPropagation()}
                  onClick={(event) => {
                    event.stopPropagation();
                    data.onRemoveNode?.(id);
                  }}
                  aria-label={`Remove ${node.name}`}
                  title="Remove"
                >
                  ×
                </button>
              ) : null}
            </div>
          ) : null}
        </div>
      ) : null}

      {isFlowNode && isMergedFlow ? (
        <div className="canvas-node-flow-merged-header" aria-hidden="true">
          <span className="canvas-node-flow-service">{node.serviceLabel}</span>
          <span className="canvas-node-flow-divider" />
        </div>
      ) : null}

      {isFlowNode ? (
        <div className="canvas-node-arrow" aria-hidden="true" />
      ) : null}

      {isAction && actionParameterSummaries.length > 0 ? (
        <div className="canvas-node-action-args nodrag" aria-label="Action properties">
          {actionParameterSummaries.slice(0, 2).map((entry) => (
            <button
              key={entry.id}
              type="button"
              className="canvas-node-action-arg nodrag"
              onPointerDown={(event) => event.stopPropagation()}
              onMouseDown={(event) => event.stopPropagation()}
              onClick={(event) => {
                event.stopPropagation();
                data.onShowActionParameterDetail?.({
                  nodeId: id,
                  nodeName: node.name,
                  nodeTypeLabel: node.typeLabel,
                  parameterId: entry.propertyId,
                  parameterName: entry.name,
                  parameterType: entry.valueType,
                  parameterValue: entry.value,
                });
              }}
            >
              {entry.name}
            </button>
          ))}
          {actionParameterSummaries.length > 2 ? (
            <span
              className="canvas-node-action-arg canvas-node-action-arg-ellipsis nodrag"
              aria-hidden="true"
            >
              ...
            </span>
          ) : null}
        </div>
      ) : null}

      <span className="canvas-node-label">{node.name}</span>
      <span className="canvas-node-meta">{node.typeLabel}</span>
      {node.isNegated ? (
        <span className="canvas-node-badge" aria-label="Negated predicate">
          NOT
        </span>
      ) : null}

      {isAction ? (
        <div className="canvas-node-pre-eff" aria-label="Preconditions and Effects">
          <button
            type="button"
            className="canvas-node-pre-eff-box nodrag"
            onMouseDown={(event) => event.stopPropagation()}
            onClick={(event) => {
              event.stopPropagation();
              data.onOpenPredicateModal?.(id, "precondition");
            }}
            aria-label={`Edit preconditions for ${node.name}`}
          >
            Pre
          </button>
          <button
            type="button"
            className="canvas-node-pre-eff-box nodrag"
            onMouseDown={(event) => event.stopPropagation()}
            onClick={(event) => {
              event.stopPropagation();
              data.onOpenPredicateModal?.(id, "effect");
            }}
            aria-label={`Edit effects for ${node.name}`}
          >
            Eff
          </button>
        </div>
      ) : null}

      {(Object.keys(portPositions) as PortSide[]).map((side) => (
        <span
          key={`port-${side}`}
          className={`canvas-node-port canvas-node-port-${side}`}
          style={{ ...PORT_STYLES[side], ...(portStyleOverrides[side] ?? {}) }}
        />
      ))}

      {(Object.keys(portPositions) as PortSide[]).map((side) => (
        <Handle
          key={`target-${side}`}
          type="target"
          position={portPositions[side]}
          id={`target-${side}`}
          className="canvas-node-handle canvas-node-handle-hitbox canvas-node-handle-target"
          style={{
            ...TARGET_HANDLE_STYLES[side],
            ...(targetHandleOverrides[side] ?? {}),
          }}
          isConnectableStart={false}
        />
      ))}

      {shouldRenderSourceHandles &&
        (Object.keys(portPositions) as PortSide[]).map((side) => (
          <Handle
            key={`source-${side}`}
            type="source"
            position={portPositions[side]}
            id={`source-${side}`}
            className="canvas-node-handle canvas-node-handle-hitbox canvas-node-handle-source"
            style={{
              zIndex: 10,
              ...SOURCE_HANDLE_STYLES[side],
              ...(sourceHandleOverrides[side] ?? {}),
            }}
            isConnectableEnd={false}
          />
        ))}
    </div>
  );
}

/**
 * behavior tree edge component for the editor canvas.
 * @param param0 component props 
 * @returns JSX element 
 */
function BehaviorEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  markerEnd,
  style,
  data,
  selected,
}: EdgeProps<BehaviorEdgeData>) {
  const [edgePath, midX, midY] = getSmoothStepPath({
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
  });

  const isMeetsEdge =
    data?.kind === "relation" &&
    typeof data?.label === "string" &&
    data.label.toLowerCase().includes("meet");

  const edgeLabel = isMeetsEdge ? "MEET" : null;

  return (
    <>
      <BaseEdge
        id={id}
        path={edgePath}
        markerEnd={markerEnd}
        interactionWidth={20} 
        style={{
          stroke: "var(--text-primary)",
          strokeWidth: 2,
          strokeDasharray: isMeetsEdge ? "6 6" : undefined,
          ...style,
        }}
      />

      {edgeLabel ? (
        <EdgeLabelRenderer>
          <div
            className="canvas-edge-label"
            style={{
              position: "absolute",
              left: midX,
              top: midY,
              transform: "translate(-50%, -50%)",
              pointerEvents: "none",
            }}
          >
            {edgeLabel}
          </div>
        </EdgeLabelRenderer>
      ) : null}

      {data?.onRemoveConnection ? (
        <EdgeLabelRenderer>
          <div
            className={`canvas-connection-remove-wrap${
              data?.isHovered ? " is-visible" : ""
            }`}
            style={{
              position: "absolute",
              left: midX,
              top: midY,
              transform: "translate(-50%, -50%)",
              pointerEvents: "all",
            }}
          >
            <button
              type="button"
              className="canvas-connection-remove-btn"
              onClick={(event) => {
                event.stopPropagation();
                data.onRemoveConnection?.(id);
              }}
              aria-label="Remove connection"
              tabIndex={data?.isHovered || selected ? 0 : -1}
            >
              ×
            </button>
          </div>
        </EdgeLabelRenderer>
      ) : null}
    </>
  );
}

function SeparatorNode({ id, data, selected }: NodeProps<SeparatorNodeData>) {
  return (
    <div className="canvas-separator" role="separator" aria-label={data.label}>
      <span className="canvas-separator-label">{data.label}</span>
      <span className="canvas-separator-line" aria-hidden="true" />

      {data.onRemoveSeparator ? (
        <span
          className={`canvas-separator-remove-wrap${
            data.isHovered || selected ? " is-visible" : ""
          }`}
        >
          <button
            type="button"
            className="canvas-separator-remove-btn"
            onMouseDown={(event) => event.stopPropagation()}
            onClick={(event) => {
              event.stopPropagation();
              data.onRemoveSeparator?.(id);
            }}
            aria-label="Remove separator"
            tabIndex={data.isHovered || selected ? 0 : -1}
          >
            ×
          </button>
        </span>
      ) : null}
    </div>
  );
}

function SubtreeNode({ data }: NodeProps<SubtreeNodeData>) {
  const title = data.node.subtreeTitle ?? "";
  const isPlanningService = title === "Planning Service";
  
  return (
    <div className={`canvas-subtree${isPlanningService ? " canvas-subtree-service" : ""}`} aria-label="Subtree">
      {/* Invisible handle at top center for flow node connections */}
      <Handle
        type="target"
        position={Position.Top}
        id="target-top"
        className="canvas-subtree-handle"
        style={{
          top: 0,
          left: "50%",
          transform: "translate(-50%, -50%)",
          opacity: 0,
          width: 20,
          height: 20,
          background: "transparent",
          border: "none",
        }}
      />
      {title ? (
        <div className="canvas-subtree-title">
          {title}
          {isPlanningService ? <span className="canvas-subtree-plus" aria-hidden="true">⊕</span> : null}
        </div>
      ) : null}
    </div>
  );
}

const nodeTypes: NodeTypes = {
  btNode: BehaviorTreeNode,
  separator: SeparatorNode,
  subtree: SubtreeNode,
};
const edgeTypes: EdgeTypes = { btEdge: BehaviorEdge };

/**
 * editor canvas component for behavior tree nodes and connections.
 * @param props component props 
 * @returns JSX element 
 */
function EditorCanvasInner(props: EditorCanvasProps) {
  const {
    nodes,
    separators = [],
    connections = [],
    rootNodeId = null,
    onDropNode,
    onDropSeparator,
    onBeginMoveNode,
    onBeginMoveSeparator,
    onMoveNode,
    onMoveSeparator,
    onResizeNode,
    onRemoveNode,
    onRemoveSeparator,
    onEditNode,
    onAddConnection,
    onRemoveConnection,
    onCycleFlowSuccessType,
    onSetRootNode,
    actionTypes,
    actionInstances,
    onShowActionParameterDetail,
    onOpenPredicateModal,
    tickStatus,
    fitViewNodeIds,
    fitViewMaxZoom,
    readOnly,
  } = props;

  const wrapperRef = useRef<HTMLDivElement>(null);
  const reactFlowInstance = useReactFlow();
  const { project } = reactFlowInstance;
  const viewportTransform = useStore((state) => state.transform);
  const viewportWidth = useStore((state) => state.width);
  const [isActive, setIsActive] = useState(false);
  const [hoveredEdgeId, setHoveredEdgeId] = useState<string | null>(null);
  const [hoveredSeparatorId, setHoveredSeparatorId] = useState<string | null>(
    null
  );

  const actionTypeMap = useMemo(() => {
    const entries = actionTypes ?? [];
    return new Map(entries.map((type) => [type.id, type] as const));
  }, [actionTypes]);

  const actionInstanceMap = useMemo(() => {
    const entries = actionInstances ?? [];
    return new Map(entries.map((instance) => [instance.id, instance] as const));
  }, [actionInstances]);

  // When fitViewNodeIds is provided, animate the viewport to fit those nodes.
  const fitViewIdsKey = fitViewNodeIds?.join(",") ?? "";
  useEffect(() => {
    if (!fitViewNodeIds?.length) return;
    // Small delay so ReactFlow has laid out the nodes first
    const timer = setTimeout(() => {
      reactFlowInstance.fitView({
        nodes: fitViewNodeIds.map((id) => ({ id })),
        padding: 0.25,
        maxZoom: fitViewMaxZoom ?? 1,
        duration: 300,
      });
    }, 50);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fitViewIdsKey, fitViewMaxZoom, reactFlowInstance]);

  const separatorFlowNodes = useMemo<FlowNode<SeparatorNodeData>[]>(() => {
    if (!separators.length) {
      return [];
    }

    const sorted = [...separators].sort((a, b) => a.y - b.y);

    const transformX = viewportTransform[0];
    const zoom = viewportTransform[2];
    const safeZoom = Number.isFinite(zoom) && zoom > 0 ? zoom : 1;
    const hasViewportSize = Number.isFinite(viewportWidth) && viewportWidth > 0;
    const viewportLeftX = -transformX / safeZoom;
    const viewportSpanWidth = hasViewportSize
      ? viewportWidth / safeZoom
      : CANVAS_EXTENT[1][0] - CANVAS_EXTENT[0][0];

    return sorted.map((separator, index) => {
      const label = `Level ${index + 1}`;
      const separatorHeight = 44;
      return {
        id: separator.id,
        type: "separator" as const,
        position: { x: viewportLeftX, y: separator.y },
        data: {
          label,
          onRemoveSeparator,
          isHovered: hoveredSeparatorId === separator.id,
        },
        draggable: true,
        selectable: true,
        focusable: true,
        connectable: false,
        width: viewportSpanWidth,
        height: separatorHeight,
        style: {
          width: `${viewportSpanWidth}px`,
          height: `${separatorHeight}px`,
        },
      } satisfies FlowNode<SeparatorNodeData>;
    });
  }, [hoveredSeparatorId, onRemoveSeparator, separators, viewportTransform, viewportWidth]);

  const behaviorFlowNodes = useMemo<FlowNode[]>(
    () => {
      const isStructuralSubtree = (node: CanvasNode) =>
        !!node.renderAsSubtree || node.typeLabel === "NodeGraph";

      const subtreeNodes = nodes.filter(isStructuralSubtree);
      const regularNodes = nodes.filter((node) => !isStructuralSubtree(node));

      const mapNode = (node: CanvasNode): FlowNode => {
        const width = node.width ?? DEFAULT_CANVAS_NODE_WIDTH;
        const height = node.height ?? DEFAULT_CANVAS_NODE_HEIGHT;

        if (isStructuralSubtree(node)) {
        return {
          id: node.id,
          type: "subtree" as const,
          position: { x: node.x - width / 2, y: node.y - height / 2 },
          data: { node },
          draggable: false,
          selectable: false,
          focusable: false,
          connectable: false,
          hidden: !!node.hidden,
          width,
          height,
          style: {
            width: `${width}px`,
            height: `${height}px`,
            zIndex: 0,
            pointerEvents: "none",
            },
          } satisfies FlowNode<SubtreeNodeData>;
        }

        return {
          id: node.id,
          type: "btNode" as const,
          position: { x: node.x - width / 2, y: node.y - height / 2 },
          data: {
            node,
            rootNodeId,
            tickStatus: tickStatus?.[node.name],
            actionTypeMap,
            actionInstanceMap,
            onRemoveNode,
            onEditNode,
            onCycleFlowSuccessType,
            onSetRootNode,
            onResizeNode,
            onMoveNode,
            onShowActionParameterDetail,
            onOpenPredicateModal,
          },
          hidden: !!node.hidden,
          width,
          height,
          style: {
            width: `${width}px`,
            height: `${height}px`,
          },
        } satisfies FlowNode<BehaviorNodeData>;
      };

      // Render subtree containers first so they sit behind the actual nodes.
      return [...subtreeNodes.map(mapNode), ...regularNodes.map(mapNode)];
    },
    [
      nodes,
      rootNodeId,
      tickStatus,
      actionTypeMap,
      actionInstanceMap,
      onRemoveNode,
      onEditNode,
      onCycleFlowSuccessType,
      onSetRootNode,
      onResizeNode,
      onMoveNode,
      onShowActionParameterDetail,
      onOpenPredicateModal,
    ]
  );

  const flowNodes = useMemo(() => {
    if (!separatorFlowNodes.length) {
      return behaviorFlowNodes;
    }

    return [...separatorFlowNodes, ...behaviorFlowNodes];
  }, [behaviorFlowNodes, separatorFlowNodes]);

  /**
   * maps canvas connections to react-flow edges.
   * @returns array of react-flow edges
   */
  const flowEdges = useMemo<FlowEdge<BehaviorEdgeData>[]>(
    () =>
      connections.map((connection) => ({
        id: connection.id,
        source: connection.sourceNodeId,
        target: connection.targetNodeId,
        sourceHandle: connection.sourcePort
          ? `source-${connection.sourcePort}`
          : undefined,
        targetHandle: connection.targetPort
          ? `target-${connection.targetPort}`
          : undefined,
        type: "btEdge" as const,
        animated: false,
        data: {
          onRemoveConnection,
          isHovered: hoveredEdgeId === connection.id,
          kind: connection.kind,
          label: connection.label,
        },
        markerEnd: {
          type: MarkerType.ArrowClosed,
          color: "var(--text-primary)",
          width: 16,
          height: 16,
        },
      })),
    [connections, hoveredEdgeId, onRemoveConnection]
  );

  /**
   * handles drag over events on the canvas.
   * @param event drag event
   */
  const handleDragOver: React.DragEventHandler<HTMLDivElement> = useCallback(
    (event) => {
      if (
        !isSidebarDrag(event.dataTransfer.types) &&
        !isCanvasToolDrag(event.dataTransfer.types)
      ) {
        return;
      }

      event.preventDefault();
      event.dataTransfer.dropEffect = "copy";
      setIsActive(true);
    },
    []
  );

  /**
   * handles drag leave events on the canvas.
   * @param event drag event
   */
  const handleDragLeave: React.DragEventHandler<HTMLDivElement> = useCallback(
    (event) => {
      const nextTarget = event.relatedTarget;
      if (
        nextTarget instanceof Element &&
        wrapperRef.current?.contains(nextTarget)
      ) {
        return;
      }

      setIsActive(false);
    },
    []
  );

  /**
   * handles drop events on the canvas.
   * @param event drop event
   */
  const handleDrop: React.DragEventHandler<HTMLDivElement> = useCallback(
    (event) => {
      const types = event.dataTransfer.types;
      const hasSidebarPayload = isSidebarDrag(types);
      const hasToolPayload = isCanvasToolDrag(types);

      if (!hasSidebarPayload && !hasToolPayload) {
        return;
      }

      event.preventDefault();
      setIsActive(false);

      try {
        const bounds = wrapperRef.current?.getBoundingClientRect();
        if (!bounds) {
          return;
        }

        const position = project({
          x: event.clientX - bounds.left,
          y: event.clientY - bounds.top,
        });

        if (hasSidebarPayload) {
          const rawPayload = event.dataTransfer.getData(DRAG_DATA_FORMAT);
          if (!rawPayload) {
            return;
          }

          const payload = JSON.parse(rawPayload) as DraggedSidebarItem;
          onDropNode(payload, position);
          return;
        }

        if (hasToolPayload) {
          const rawPayload = event.dataTransfer.getData(
            CANVAS_TOOL_DRAG_DATA_FORMAT
          );
          if (!rawPayload) {
            return;
          }

          const payload = JSON.parse(rawPayload) as DraggedCanvasTool;
          if (payload.tool === "separatorLine") {
            onDropSeparator?.(position);
          }
        }
      } catch (error) {
        console.error("Failed to parse sidebar drag payload", error);
      }
    },
    [onDropNode, onDropSeparator, project]
  );

  /**
   * handles connection events on the canvas.
   * @param connection connection data from react-flow
   */
  const handleConnect = useCallback(
    (connection: Connection) => {
      if (!onAddConnection || !connection.source || !connection.target) {
        return;
      }

      const sourceNode = nodes.find((entry) => entry.id === connection.source);
      const targetNode = nodes.find((entry) => entry.id === connection.target);

      const isAction = (node: CanvasNode) =>
        node.kind === "actionType" || node.kind === "actionInstance";
      const isFlow = (node: CanvasNode) => node.category === FLOW_NODES_KEY;

      // Rules:
      // - Flow -> (Flow/Action/Service/Decorator/anything non-flow) is allowed (structure).
      // - Action -> Action is allowed (plan/order/temporal relations).
      // - Non-flow -> Flow is not allowed.
      if (!sourceNode || !targetNode) {
        return;
      }

      if (isFlow(targetNode) && !isFlow(sourceNode)) {
        return;
      }

      if (isFlow(sourceNode)) {
        // allow Flow -> non-flow
      } else if (isAction(sourceNode) && isAction(targetNode)) {
        // allow Action -> Action (plan graph)
      } else {
        return;
      }

      const allPorts: PortSide[] = ["top", "right", "bottom", "left"];
      const sourcePorts = allPorts;
      const targetPorts = isFlow(targetNode) ? (["top"] as PortSide[]) : allPorts;
      const best = chooseShortestPorts(sourceNode, targetNode, sourcePorts, targetPorts);
      const sourcePort = best.sourcePort;
      const targetPort = best.targetPort;
      onAddConnection(
        connection.source,
        connection.target,
        sourcePort,
        targetPort
      );
    },
    [nodes, onAddConnection]
  );

  const handleEdgeMouseEnter = useCallback(
    (_event: React.MouseEvent, edge: FlowEdge) => {
      setHoveredEdgeId(edge.id);
    },
    []
  );

  const handleEdgeMouseLeave = useCallback(() => {
    setHoveredEdgeId(null);
  }, []);

  /**
   * handles node drag events on the canvas.
   * @param event mouse event
   * @param node dragged node data
   */
  const handleNodeDrag = useCallback(
    (_event: React.MouseEvent, node: FlowNode) => {
      if (node.type === "separator") {
        onMoveSeparator?.(node.id, node.position.y);
        return;
      }

      const width = node.width ?? DEFAULT_CANVAS_NODE_WIDTH;
      const height = node.height ?? DEFAULT_CANVAS_NODE_HEIGHT;
      onMoveNode?.(node.id, {
        x: node.position.x + width / 2,
        y: node.position.y + height / 2,
      });
    },
    [onMoveNode, onMoveSeparator]
  );

  const handleNodeDragStart = useCallback(
    (_event: React.MouseEvent, node: FlowNode) => {
      if (node.type === "separator") {
        onBeginMoveSeparator?.(node.id);
        return;
      }

      onBeginMoveNode?.(node.id);
    },
    [onBeginMoveNode, onBeginMoveSeparator]
  );

  /**
   * handles node drag stop events on the canvas.
   * @param event mouse event
   * @param node dragged node data
   */
  const handleNodeDragStop = useCallback(
    (_event: React.MouseEvent, node: FlowNode) => {
      if (node.type === "separator") {
        onMoveSeparator?.(node.id, node.position.y);
        return;
      }

      const width = node.width ?? DEFAULT_CANVAS_NODE_WIDTH;
      const height = node.height ?? DEFAULT_CANVAS_NODE_HEIGHT;
      onMoveNode?.(node.id, {
        x: node.position.x + width / 2,
        y: node.position.y + height / 2,
      });
    },
    [onMoveNode, onMoveSeparator]
  );

  const handleNodeMouseEnter = useCallback(
    (_event: React.MouseEvent, node: FlowNode) => {
      if (node.type !== "separator") {
        return;
      }

      setHoveredSeparatorId(node.id);
    },
    []
  );

  const handleNodeMouseLeave = useCallback(
    (_event: React.MouseEvent, node: FlowNode) => {
      if (node.type !== "separator") {
        return;
      }

      setHoveredSeparatorId((current) => (current === node.id ? null : current));
    },
    []
  );

  return (
    <div
      ref={wrapperRef}
      className={`editor-canvas${isActive ? " is-active" : ""}`}
      onDragOver={readOnly ? undefined : handleDragOver}
      onDragLeave={readOnly ? undefined : handleDragLeave}
      onDrop={readOnly ? undefined : handleDrop}
    >
      <ReactFlow
        nodes={flowNodes}
        edges={flowEdges}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        translateExtent={CANVAS_EXTENT}
        nodeExtent={CANVAS_EXTENT}
        onConnect={readOnly ? undefined : handleConnect}
        onEdgeMouseEnter={readOnly ? undefined : handleEdgeMouseEnter}
        onEdgeMouseLeave={readOnly ? undefined : handleEdgeMouseLeave}
        onNodeMouseEnter={readOnly ? undefined : handleNodeMouseEnter}
        onNodeMouseLeave={readOnly ? undefined : handleNodeMouseLeave}
        onNodeDragStart={readOnly ? undefined : handleNodeDragStart}
        onNodeDrag={readOnly ? undefined : handleNodeDrag}
        onNodeDragStop={readOnly ? undefined : handleNodeDragStop}
        connectionLineType={ConnectionLineType.SmoothStep}
        proOptions={{ hideAttribution: true }}
        minZoom={0.05}
        maxZoom={4}
        panOnDrag
        fitView
        nodesDraggable={!readOnly}
        nodesConnectable={!readOnly}
        nodesFocusable={!readOnly}
        elementsSelectable={!readOnly}
      >
        <Background
          variant={BackgroundVariant.Cross}
          gap={32}
          size={2}
          color="rgba(99, 102, 241, 0.25)"
        />
      </ReactFlow>
    </div>
  );
}

export default function EditorCanvas(props: EditorCanvasProps) {
  return (
    <ReactFlowProvider>
      <EditorCanvasInner {...props} />
    </ReactFlowProvider>
  );
}
