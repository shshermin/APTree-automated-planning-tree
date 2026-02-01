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
  actionTypeMap: Map<string, ActionType>;
  actionInstanceMap: Map<string, ActionInstance>;
  onRemoveNode?: (nodeId: string) => void;
  onEditNode?: (nodeId: string) => void;
  onCycleFlowSuccessType?: (nodeId: string) => void;
  onResizeNode?: (nodeId: string, size: { width: number; height: number }) => void;
  onMoveNode?: (nodeId: string, position: { x: number; y: number }) => void;
  onShowActionParameterDetail?: (detail: ActionParameterDetail) => void;
}

interface BehaviorEdgeData {
  onRemoveConnection?: (connectionId: string) => void;
  isHovered?: boolean;
}

interface SeparatorNodeData {
  label: string;
  onRemoveSeparator?: (separatorId: string) => void;
  isHovered?: boolean;
}

const portPositions: Record<PortSide, Position> = {
  top: Position.Top,
  right: Position.Right,
  bottom: Position.Bottom,
  left: Position.Left,
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
  [-4000, -4000],
  [4000, 4000],
];

const PARAM_BOX_HEIGHT = 24;
const PARAM_BOX_GAP = 6;
const PARAM_STACK_CLEARANCE = 18;
const SUCCESS_BADGE_CLEARANCE = 32;

/**
 * resolves the port side from a handle ID string.
 * @param handleId the handle ID string from react-flow 
 * @param fallback the fallback port side if none can be resolved 
 * @returns the resolved port side 
 */
function resolvePortFromHandle(
  handleId: string | null | undefined,
  fallback: PortSide
): PortSide {
  if (!handleId) {
    return fallback;
  }

  const match = handleId.match(/(top|right|bottom|left)$/);
  if (!match) {
    return fallback;
  }

  return match[1] as PortSide;
}

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

  if (node.category === FLOW_NODES_KEY) {
    nodeClasses.push("canvas-node-flow");
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
    const offset = "3.75%";

    portStyleOverrides.left = { left: offset };
    portStyleOverrides.right = { right: offset };

    sourceHandleOverrides.left = { left: offset };
    sourceHandleOverrides.right = { right: offset };

    targetHandleOverrides.left = { left: offset };
    targetHandleOverrides.right = { right: offset };
  }

  const successBadgeClearance = node.successType ? SUCCESS_BADGE_CLEARANCE : 0;
  const topClearance = (isAction ? paramClearance : 0) + successBadgeClearance;

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

      {showActionParams ? (
        <div className="canvas-node-params" aria-label="Action parameters">
          {actionParameterSummaries.map((entry) => (
            <button
              key={entry.id}
              type="button"
              className="canvas-node-params-chip"
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
        </div>
      ) : null}

      {node.successType ? (
        data.onCycleFlowSuccessType ? (
          <button
            type="button"
            className="canvas-node-success"
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
        )
      ) : null}

      {data.onEditNode ? (
        <button
          type="button"
          className="canvas-node-edit"
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
          className="canvas-node-remove"
          onMouseDown={(event) => event.stopPropagation()}
          onClick={(event) => {
            event.stopPropagation();
            data.onRemoveNode?.(id);
          }}
          aria-label={`Remove ${node.name}`}
        >
          ×
        </button>
      ) : null}

      <span className="canvas-node-label">{node.name}</span>
      <span className="canvas-node-meta">{node.typeLabel}</span>
      {node.isNegated ? (
        <span className="canvas-node-badge" aria-label="Negated predicate">
          NOT
        </span>
      ) : null}

      {isAction ? (
        <div className="canvas-node-state" />
      ) : null}

      {(Object.keys(portPositions) as PortSide[]).map((side) => (
        <span
          key={`port-${side}`}
          className={`canvas-node-port canvas-node-port-${side}`}
          style={{ ...PORT_STYLES[side], ...(portStyleOverrides[side] ?? {}) }}
        />
      ))}

      {!isFlowNode
        ? (Object.keys(portPositions) as PortSide[]).map((side) => (
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
          ))
        : null}

      {(isFlowNode || isAction) &&
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

  return (
    <>
      <BaseEdge
        id={id}
        path={edgePath}
        markerEnd={markerEnd}
        interactionWidth={20} 
        style={{
          stroke: "#fff",
          strokeWidth: 2,
          ...style,
        }}
      />
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

const nodeTypes: NodeTypes = { btNode: BehaviorTreeNode, separator: SeparatorNode };
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
    onDropNode,
    onDropSeparator,
    onMoveNode,
    onMoveSeparator,
    onResizeNode,
    onRemoveNode,
    onRemoveSeparator,
    onEditNode,
    onAddConnection,
    onRemoveConnection,
    onCycleFlowSuccessType,
    actionTypes,
    actionInstances,
    onShowActionParameterDetail,
  } = props;

  const wrapperRef = useRef<HTMLDivElement>(null);
  const { project } = useReactFlow();
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

  const behaviorFlowNodes = useMemo<FlowNode<BehaviorNodeData>[]>(
    () =>
      nodes.map((node) => {
        const width = node.width ?? DEFAULT_CANVAS_NODE_WIDTH;
        const height = node.height ?? DEFAULT_CANVAS_NODE_HEIGHT;

        return {
          id: node.id,
          type: "btNode" as const,
          position: { x: node.x - width / 2, y: node.y - height / 2 },
          data: {
            node,
            actionTypeMap,
            actionInstanceMap,
            onRemoveNode,
            onEditNode,
            onCycleFlowSuccessType,
            onResizeNode,
            onMoveNode,
            onShowActionParameterDetail,
          },
          width,
          height,
        } satisfies FlowNode<BehaviorNodeData>;
      }),
    [
      nodes,
      actionTypeMap,
      actionInstanceMap,
      onRemoveNode,
      onEditNode,
      onCycleFlowSuccessType,
      onResizeNode,
      onMoveNode,
      onShowActionParameterDetail,
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
        },
        markerEnd: {
          type: MarkerType.ArrowClosed,
          color: "#fff",
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
      // - Flow -> (Action/Service/Decorator/anything non-flow) is allowed (structure).
      // - Action -> Action is allowed (plan/order/temporal relations).
      // - Anything -> Flow is not allowed.
      // - Flow -> Flow is not allowed.
      if (!sourceNode || !targetNode) {
        return;
      }

      if (isFlow(targetNode)) {
        return;
      }

      if (isFlow(sourceNode)) {
        // allow Flow -> non-flow
      } else if (isAction(sourceNode) && isAction(targetNode)) {
        // allow Action -> Action (plan graph)
      } else {
        return;
      }

      const sourcePort = resolvePortFromHandle(connection.sourceHandle, "right");
      const targetPort = resolvePortFromHandle(connection.targetHandle, "left");
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
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      <ReactFlow
        nodes={flowNodes}
        edges={flowEdges}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        translateExtent={CANVAS_EXTENT}
        nodeExtent={CANVAS_EXTENT}
        onConnect={handleConnect}
        onEdgeMouseEnter={handleEdgeMouseEnter}
        onEdgeMouseLeave={handleEdgeMouseLeave}
        onNodeMouseEnter={handleNodeMouseEnter}
        onNodeMouseLeave={handleNodeMouseLeave}
        onNodeDrag={handleNodeDrag}
        onNodeDragStop={handleNodeDragStop}
        connectionLineType={ConnectionLineType.SmoothStep}
        proOptions={{ hideAttribution: true }}
        panOnDrag
        fitView
        nodesDraggable
        nodesConnectable
        nodesFocusable
        elementsSelectable
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
