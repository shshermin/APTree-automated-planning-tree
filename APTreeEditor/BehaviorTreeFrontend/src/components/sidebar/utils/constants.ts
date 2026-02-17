import type {
  AppData,
  CategoryConfig,
  DataCategory,
  DecoratorNodeOption,
  FlowNodeOption,
  NodeGraphNodeOption,
  ServiceNodeOption,
} from "./types";

/**
 * fallback flow-node definitions.
 * Used only when the backend catalog endpoint is unavailable.
 */
export const FALLBACK_FLOW_NODE_OPTIONS: FlowNodeOption[] = [
  {
    id: "BTFlowNode_Composite",
    label: "Composite Flow Node",
    typeLabel: "Flow",
    description: "A composite flow node that can contain action and flow children.",
    kind: "flow",
    defaultSuccessType: "ALL",
  },
  {
    id: "BTFlowNode_Dynamic",
    label: "Dynamic Flow Node",
    typeLabel: "Flow",
    description: "A dynamic flow node that builds its graph at runtime.",
    kind: "flow",
    defaultSuccessType: "ALL",
  },
];

/**
 * fallback decorator-node definitions.
 * Used only when the backend catalog endpoint is unavailable.
 */
export const FALLBACK_DECORATOR_NODE_OPTIONS: DecoratorNodeOption[] = [
  {
    id: "inverter",
    label: "Inverter",
    typeLabel: "Decorator",
    description:
      "Flip the child node result from success to failure and vice versa.",
    kind: "decorator",
  },
  {
    id: "repeat-until-success",
    label: "Repeat Until Success",
    typeLabel: "Decorator",
    description:
      "Retry the child node until it succeeds or reaches a retry limit.",
    kind: "decorator",
  },
  {
    id: "cooldown",
    label: "Cooldown",
    typeLabel: "Decorator",
    description:
      "Ensure the child node executes only after a specified cooldown period.",
    kind: "decorator",
  },
];

/**
 * fallback service-node definitions.
 * Used only when the backend catalog endpoint is unavailable.
 */
export const FALLBACK_SERVICE_NODE_OPTIONS: ServiceNodeOption[] = [
  {
    id: "sensing-service",
    label: "Sensing Service",
    typeLabel: "Service",
    description:
      "Run periodic sensor checks alongside the behavior tree branch.",
    kind: "service",
  },
  {
    id: "blackboard-sync",
    label: "Blackboard Sync",
    typeLabel: "Service",
    description:
      "Continuously synchronize key values into the blackboard while active.",
    kind: "service",
  },
];

/**
 * fallback node-graph definitions.
 * NodeGraphs are structural container nodes holding a plan subgraph.
 */
export const FALLBACK_NODEGRAPH_NODE_OPTIONS: NodeGraphNodeOption[] = [];

export const BLACKBOARD_KEY: DataCategory = "variables";
export const BT_NODES_KEY: DataCategory = "nodes";
export const DECORATOR_NODES_KEY: DataCategory = "decorators";
export const SERVICE_NODES_KEY: DataCategory = "services";

/**
 * central configuration describing each sidebar category including labels and defaults.
 * @returns ordered category configuration list consumed across the sidebar
 */
export const CATEGORY_CONFIG: CategoryConfig[] = [
  {
    key: BLACKBOARD_KEY,
    title: "Blackboard",
    addLabel: "Add Variable",
    defaultItems: [
      { id: "variable-health", name: "health", type: "Integer" },
      { id: "variable-target", name: "target", type: "Agent" },
    ],
  },
  {
    key: BT_NODES_KEY,
    title: "Behavior Tree Nodes",
    addLabel: "Add Behavior Node",
  },
  {
    key: "paramTypes",
    title: "Parameter Types",
    addLabel: "Add Parameter Type",
  },
  {
    key: "predTypes",
    title: "Predicate Types",
    addLabel: "Add Predicate Type",
  },
  { key: "actions", title: "Action Types", addLabel: "Add Action Type" },
];

/**
 * provides default data entries mapped by category, cloning default items where available.
 * @returns hydrated data map keyed by category identifiers
 */
export const DEFAULT_DATA: AppData = CATEGORY_CONFIG.reduce<AppData>(
  (acc, section) => {
    const defaults = section.defaultItems ?? [];
    acc[section.key] = defaults.map((item) => ({ ...item }));
    return acc;
  },
  {} as AppData
);

/**
 * maps each category key to its display title for quick lookup.
 * @returns immutable dictionary mapping category to title
 */
export const DEFAULT_TITLES = CATEGORY_CONFIG.reduce<Record<string, string>>(
  (acc, section) => {
    acc[section.key] = section.title;
    return acc;
  },
  {}
);

/**
 * maps category keys to their associated "add" button labels.
 * @returns dictionary of add button captions
 */
export const ADD_LABELS = CATEGORY_CONFIG.reduce<Record<string, string>>(
  (acc, section) => {
    acc[section.key] = section.addLabel;
    return acc;
  },
  {}
);

/**
 * lists the default rendering order of categories in the sidebar.
 * @returns array of category keys sorted for initial render
 */
export const DEFAULT_ORDER = CATEGORY_CONFIG.map((section) => section.key);

export const PARAM_TYPES_KEY: DataCategory = "paramTypes";
export const PREDICATE_TYPES_KEY: DataCategory = "predTypes";
export const ACTION_TYPES_KEY: DataCategory = "actions";
export const ACTION_INSTANCES_KEY: DataCategory = "actionInstances";
export const FLOW_NODES_KEY: DataCategory = "flowNodes";
export const DRAGGABLE_NODE_CATEGORIES: readonly DataCategory[] = [
  ACTION_INSTANCES_KEY,
];
