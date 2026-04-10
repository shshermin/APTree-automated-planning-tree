import { useCallback, useEffect, useMemo, useRef, useState } from "react";

/**
 * Connects to the backend WebSocket at /ws/tick and returns a live map of
 * nodeName → latest tick status ("Running" | "Success" | "Failure").
 * Statuses expire after EXPIRE_MS milliseconds so nodes don't stay highlighted forever.
 */
function useTickStatus(expireMs = 2000): Record<string, string> {
  const [tickStatus, setTickStatus] = useState<Record<string, string>>({});
  const timersRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());
  const wsRef = useRef<WebSocket | null>(null);

  useEffect(() => {
    let active = true;

    function connect() {
      if (!active) return;
      const protocol = window.location.protocol === "https:" ? "wss:" : "ws:";
      const ws = new WebSocket(`${protocol}//${window.location.host}/ws/tick`);
      wsRef.current = ws;

      ws.onmessage = (event) => {
        if (!active) return;
        try {
          const { nodeName, status } = JSON.parse(event.data as string) as {
            nodeName: string;
            status: string;
          };
          setTickStatus((prev) => ({ ...prev, [nodeName]: status }));

          const existing = timersRef.current.get(nodeName);
          if (existing) clearTimeout(existing);

          const timer = setTimeout(() => {
            setTickStatus((prev) => {
              const next = { ...prev };
              delete next[nodeName];
              return next;
            });
            timersRef.current.delete(nodeName);
          }, expireMs);

          timersRef.current.set(nodeName, timer);
        } catch {
          // ignore malformed messages
        }
      };

      ws.onclose = () => {
        // Reconnect after 2s if still mounted
        if (active) setTimeout(connect, 2000);
      };
    }

    connect();

    return () => {
      active = false;
      wsRef.current?.close();
      wsRef.current = null;
      timersRef.current.forEach(clearTimeout);
      timersRef.current.clear();
    };
  }, [expireMs]);

  return tickStatus;
}
import "./App.css";
import Header from "./components/header/Header.tsx";
import Sidebar from "./components/sidebar/Sidebar.tsx";
import { useSidebarManager } from "./components/sidebar/useSidebarLogic";
import EditorCanvas from "./components/editor/EditorCanvas.tsx";
import type {
  ActionParameterDetail,
  CanvasSeparator,
  CanvasNode,
  NodeConnection,
  PredicateGroup,
  PredicateInstance,
} from "./components/editor/types";
import {
  DEFAULT_CANVAS_NODE_HEIGHT,
  DEFAULT_CANVAS_NODE_WIDTH,
} from "./components/editor/types";
import type { DraggedSidebarItem } from "./components/editor/dragTypes";
import { createId } from "./utils/id";
import { createBehaviorNode } from "./components/editor/flowNodeFactory";
import { reconcileInstanceValues } from "./components/sidebar/utils/helpers";
import {
  ACTION_INSTANCES_KEY,
  BT_NODES_KEY,
  FLOW_NODES_KEY,
} from "./components/sidebar/utils/constants";
import {
  aptreeGraphToCanvasGraph,
  aptreeGraphsToCanvasGraph,
  normalizeAptreeValidateResponse,
} from "./utils/aptreeImport";
import ActionParameterDetailsModal from "./components/editor/modals/ActionParameterDetailsModal.tsx";
import ActionPredicateModal from "./components/editor/modals/ActionPredicateModal.tsx";
import AptreeValidateModal from "./components/aptree/AptreeValidateModal";
import { FLOW_SUCCESS_TYPES } from "./components/sidebar/utils/types";
import type {
  ActionInstance,
  BehaviorNodeOption,
} from "./components/sidebar/utils/types";

type ThemeMode = "light" | "dark";

type CanvasGraph = {
  nodes: CanvasNode[];
  connections: NodeConnection[];
  rootNodeId: string | null;
};

type CanvasSnapshot = {
  graph: CanvasGraph;
  separators: CanvasSeparator[];
};

// legacy export/import format used when the editor had multiple level tabs.
type ExportedCanvasGraphsV1 = {
  version: 1;
  exportedAt: string;
  activeLevel: "high" | "mid" | "low";
  graphs: Record<"high" | "mid" | "low", CanvasGraph>;
};

type ExportedCanvasGraphV2 = {
  version: 2;
  exportedAt: string;
  graph: CanvasGraph;
  separators: CanvasSeparator[];
};

type PortValue = "top" | "right" | "bottom" | "left";

const isPortValue = (value: unknown): value is PortValue =>
  value === "top" || value === "right" || value === "bottom" || value === "left";

const normalizePredicateInstances = (
  value: unknown
): PredicateInstance[] => {
  if (!Array.isArray(value)) {
    return [];
  }

  const seen = new Set<string>();
  const normalized: PredicateInstance[] = [];

  for (const raw of value) {
    if (!raw || typeof raw !== "object") {
      continue;
    }

    const entry = raw as Partial<PredicateInstance>;
    const typeId = typeof entry.typeId === "string" ? entry.typeId : "";
    if (!typeId) {
      continue;
    }

    const id = typeof entry.id === "string" ? entry.id : createId("predicate");
    if (seen.has(id)) {
      continue;
    }
    const typeName = typeof entry.typeName === "string" ? entry.typeName : "";
    const propertyValues: Record<string, string> = {};

    if (entry.propertyValues && typeof entry.propertyValues === "object") {
      Object.entries(entry.propertyValues as Record<string, unknown>).forEach(
        ([key, val]) => {
          propertyValues[key] =
            typeof val === "string" ? val : val == null ? "" : String(val);
        }
      );
    }

    normalized.push({
      id,
      typeId,
      typeName,
      propertyValues,
      isNegated: typeof entry.isNegated === "boolean" ? entry.isNegated : undefined,
    });
    seen.add(id);
  }

  return normalized;
};

function normalizeImportedCanvasGraph(value: CanvasGraph): CanvasGraph {
  const seenNodeIds = new Set<string>();
  const normalizedNodes: CanvasNode[] = [];

  for (const raw of value.nodes) {
    if (!raw || typeof raw !== "object") {
      continue;
    }

    const node = raw as Partial<CanvasNode>;
    const id = typeof node.id === "string" ? node.id : "";
    if (!id || seenNodeIds.has(id)) {
      continue;
    }

    const sourceId = typeof node.sourceId === "string" ? node.sourceId : "";
    const name = typeof node.name === "string" ? node.name : "";
    const typeLabel = typeof node.typeLabel === "string" ? node.typeLabel : "";
    const category = typeof node.category === "string" ? node.category : "nodes";
    const kind = typeof node.kind === "string" ? node.kind : "generic";

    if (!sourceId || !name || !typeLabel) {
      continue;
    }

    const x = typeof node.x === "number" && Number.isFinite(node.x) ? node.x : 0;
    const y = typeof node.y === "number" && Number.isFinite(node.y) ? node.y : 0;
    const width =
      typeof node.width === "number" && Number.isFinite(node.width) && node.width > 0
        ? node.width
        : DEFAULT_CANVAS_NODE_WIDTH;
    const height =
      typeof node.height === "number" && Number.isFinite(node.height) && node.height > 0
        ? node.height
        : DEFAULT_CANVAS_NODE_HEIGHT;

    const preconditions = normalizePredicateInstances(node.preconditions);
    const effects = normalizePredicateInstances(node.effects);

    normalizedNodes.push({
      id,
      sourceId,
      name,
      typeLabel,
      category: category as CanvasNode["category"],
      kind: kind as CanvasNode["kind"],
      x,
      y,
      width,
      height,
      isNegated: node.isNegated,
      successType: node.successType,
      typeId: node.typeId,
      preconditions: preconditions.length > 0 ? preconditions : undefined,
      effects: effects.length > 0 ? effects : undefined,
    });
    seenNodeIds.add(id);
  }

  const nodeIdSet = new Set(normalizedNodes.map((n) => n.id));
  const normalizedRootNodeId =
    typeof value.rootNodeId === "string" && nodeIdSet.has(value.rootNodeId)
      ? value.rootNodeId
      : null;
  const seenConnIds = new Set<string>();
  const normalizedConnections: NodeConnection[] = [];

  for (const raw of value.connections) {
    if (!raw || typeof raw !== "object") {
      continue;
    }

    const conn = raw as Partial<NodeConnection>;
    const id = typeof conn.id === "string" ? conn.id : "";
    const sourceNodeId = typeof conn.sourceNodeId === "string" ? conn.sourceNodeId : "";
    const targetNodeId = typeof conn.targetNodeId === "string" ? conn.targetNodeId : "";

    if (!id || seenConnIds.has(id) || !sourceNodeId || !targetNodeId) {
      continue;
    }

    if (!nodeIdSet.has(sourceNodeId) || !nodeIdSet.has(targetNodeId)) {
      continue;
    }

    const sourcePort = isPortValue(conn.sourcePort) ? conn.sourcePort : undefined;
    const targetPort = isPortValue(conn.targetPort) ? conn.targetPort : undefined;

    normalizedConnections.push({
      id,
      sourceNodeId,
      targetNodeId,
      sourcePort,
      targetPort,
    });
    seenConnIds.add(id);
  }

  return {
    nodes: normalizedNodes,
    connections: normalizedConnections,
    rootNodeId: normalizedRootNodeId,
  };
}

function normalizeImportedSeparators(value: CanvasSeparator[]): CanvasSeparator[] {
  const seen = new Set<string>();
  const normalized: CanvasSeparator[] = [];

  for (const raw of value) {
    if (!raw || typeof raw !== "object") {
      continue;
    }

    const sep = raw as Partial<CanvasSeparator>;
    const id = typeof sep.id === "string" ? sep.id : "";
    const y = typeof sep.y === "number" && Number.isFinite(sep.y) ? sep.y : null;
    if (!id || y === null || seen.has(id)) {
      continue;
    }
    normalized.push({ id, y });
    seen.add(id);
  }

  return normalized;
}

const STORAGE_KEY = "aptree-preferred-theme";

/**
 * retrieves the initial theme mode based on user preference or system settings.
 * @returns initial theme mode
 */
function getInitialTheme(): ThemeMode {
  if (typeof window === "undefined") {
    return "dark";
  }

  const storedTheme = window.localStorage.getItem(
    STORAGE_KEY
  ) as ThemeMode | null;
  if (storedTheme === "light" || storedTheme === "dark") {
    return storedTheme;
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches
    ? "dark"
    : "light";
}

/**
 * application root component.
 * @returns main application element
 */
function App() {
  const tickStatus = useTickStatus();
  const [theme, setTheme] = useState<ThemeMode>(getInitialTheme);
  const [userLockedTheme, setUserLockedTheme] = useState<boolean>(() => {
    if (typeof window === "undefined") {
      return false;
    }
    const savedTheme = window.localStorage.getItem(STORAGE_KEY);
    return savedTheme === "light" || savedTheme === "dark";
  });
  const [graph, setGraph] = useState<CanvasGraph>(() => ({
    nodes: [],
    connections: [],
    rootNodeId: null,
  }));
  const [separators, setSeparators] = useState<CanvasSeparator[]>([]);
  const [undoStack, setUndoStack] = useState<CanvasSnapshot[]>([]);
  const [redoStack, setRedoStack] = useState<CanvasSnapshot[]>([]);
  const [parameterDetail, setParameterDetail] =
    useState<ActionParameterDetail | null>(null);
  const [predicateModalState, setPredicateModalState] = useState<{
    nodeId: string;
    group: PredicateGroup;
  } | null>(null);
  const [isValidateOpen, setIsValidateOpen] = useState(false);
  const sidebarManager = useSidebarManager();
  const {
    importActionInstancesFromText,
    actionTypes,
    getItemsForCategory,
    openEditModal,
    addActionTypes,
    addActionInstances,
    predicateTypes,
  } = sidebarManager;

  const rawActionInstances = useMemo(
    () => getItemsForCategory(ACTION_INSTANCES_KEY) as ActionInstance[],
    [getItemsForCategory]
  );

  const behaviorNodeOptionMap = useMemo(() => {
    const options: BehaviorNodeOption[] = [
      ...sidebarManager.flowNodeOptions,
      ...sidebarManager.decoratorNodeOptions,
      ...sidebarManager.serviceNodeOptions,
      ...sidebarManager.nodeGraphNodeOptions,
    ];
    return new Map(options.map((option) => [option.id, option] as const));
  }, [
    sidebarManager.flowNodeOptions,
    sidebarManager.decoratorNodeOptions,
    sidebarManager.serviceNodeOptions,
    sidebarManager.nodeGraphNodeOptions,
  ]);

  const actionInstances = useMemo(() => {
    if (!rawActionInstances.length) {
      return rawActionInstances;
    }

    const typeMap = new Map(
      (actionTypes ?? []).map((type) => [type.id, type] as const)
    );

    let hasChanges = false;
    const reconciled = rawActionInstances.map((instance) => {
      const definition = typeMap.get(instance.typeId);
      if (!definition) {
        if (
          !instance.propertyValues ||
          Object.keys(instance.propertyValues).length === 0
        ) {
          return instance;
        }

        hasChanges = true;
        return { ...instance, propertyValues: {} };
      }

      const nextValues = reconcileInstanceValues(
        definition,
        instance.propertyValues ?? {}
      );

      const hasSameKeys =
        Object.keys(nextValues).length ===
          Object.keys(instance.propertyValues ?? {}).length &&
        Object.entries(nextValues).every(
          ([key, value]) => instance.propertyValues?.[key] === value
        );

      if (hasSameKeys) {
        return instance;
      }

      hasChanges = true;
      return {
        ...instance,
        propertyValues: nextValues,
      };
    });

    return hasChanges ? reconciled : rawActionInstances;
  }, [rawActionInstances, actionTypes]);

  /**
   * shows the action parameter detail modal with the provided detail.
   * @param detail action parameter detail to display
   */
  const handleShowActionParameterDetail = useCallback(
    (detail: ActionParameterDetail) => {
      setParameterDetail(detail);
    },
    []
  );

  /**
   * closes the action parameter detail modal.
   */
  const handleCloseActionParameterDetail = useCallback(() => {
    setParameterDetail(null);
  }, []);

  const handleOpenPredicateModal = useCallback(
    (nodeId: string, group: PredicateGroup) => {
      setPredicateModalState({ nodeId, group });
    },
    []
  );

  const handleClosePredicateModal = useCallback(() => {
    setPredicateModalState(null);
  }, []);

  const handleChangePredicateGroup = useCallback((group: PredicateGroup) => {
    setPredicateModalState((prev) => (prev ? { ...prev, group } : prev));
  }, []);

  const graphRef = useRef(graph);
  const separatorsRef = useRef(separators);
  const lastHistoryRef = useRef<{ ts: number; reason: string } | null>(null);
  const MAX_HISTORY = 80;

  useEffect(() => {
    graphRef.current = graph;
  }, [graph]);

  useEffect(() => {
    separatorsRef.current = separators;
  }, [separators]);

  const pushHistory = useCallback((reason: string) => {
    const snapshot: CanvasSnapshot = {
      graph: graphRef.current,
      separators: separatorsRef.current,
    };
    const now = Date.now();

    setUndoStack((prev) => {
      const last = lastHistoryRef.current;
      const shouldReplace =
        last && last.reason === reason && now - last.ts < 400 && prev.length > 0;
      lastHistoryRef.current = { ts: now, reason };

      if (shouldReplace) {
        const next = prev.slice(0, -1);
        next.push(snapshot);
        return next;
      }

      const next = [...prev, snapshot];
      if (next.length > MAX_HISTORY) {
        next.shift();
      }
      return next;
    });
    setRedoStack([]);
  }, []);

  const handleUndo = useCallback(() => {
    setUndoStack((prev) => {
      if (!prev.length) {
        return prev;
      }
      const snapshot = prev[prev.length - 1];
      setRedoStack((redoPrev) => {
        const next = [
          ...redoPrev,
          { graph: graphRef.current, separators: separatorsRef.current },
        ];
        if (next.length > MAX_HISTORY) {
          next.shift();
        }
        return next;
      });
      setGraph(snapshot.graph);
      setSeparators(snapshot.separators);
      return prev.slice(0, -1);
    });
  }, []);

  const handleRedo = useCallback(() => {
    setRedoStack((prev) => {
      if (!prev.length) {
        return prev;
      }
      const snapshot = prev[prev.length - 1];
      setUndoStack((undoPrev) => {
        const next = [
          ...undoPrev,
          { graph: graphRef.current, separators: separatorsRef.current },
        ];
        if (next.length > MAX_HISTORY) {
          next.shift();
        }
        return next;
      });
      setGraph(snapshot.graph);
      setSeparators(snapshot.separators);
      return prev.slice(0, -1);
    });
  }, []);

  /**
   * applies the current theme to the document root and persists the preference.
   */
  useEffect(() => {
    const root = document.documentElement;
    root.dataset.theme = theme;
    root.style.colorScheme = theme;
    window.localStorage.setItem(STORAGE_KEY, theme);
  }, [theme]);

  /**
   * listens for system theme changes if the user has not locked their preference.
   */
  useEffect(() => {
    if (typeof window === "undefined" || userLockedTheme) {
      return;
    }

    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
    const handleSystemChange = (event: MediaQueryListEvent) => {
      setTheme(event.matches ? "dark" : "light");
    };

    if (typeof mediaQuery.addEventListener === "function") {
      mediaQuery.addEventListener("change", handleSystemChange);
      return () => mediaQuery.removeEventListener("change", handleSystemChange);
    }

    mediaQuery.addListener(handleSystemChange);
    return () => mediaQuery.removeListener(handleSystemChange);
  }, [userLockedTheme]);

  /**
   * toggles the application theme between light and dark modes.
   */
  const handleToggleTheme = () => {
    setTheme((current) => (current === "light" ? "dark" : "light"));
    setUserLockedTheme(true);
  };

  /**
   * handles importing instances from a file using the provided importer function.
   */
  const handleImportFromFile = useCallback(
    (
      file: File,
      importer: (text: string) => {
        processed: number;
        imported: number;
        skipped: number;
        errors: string[];
      },
      label: string
    ) => {
      const reader = new FileReader();
      reader.onload = () => {
        const text = typeof reader.result === "string" ? reader.result : "";
        const summary = importer(text);
        if (summary.processed === 0) {
          window.alert(`No ${label} found in the file.`);
          return;
        }

        const base = `${summary.imported} of ${summary.processed} ${label} imported.`;
        const skippedNote =
          summary.skipped > 0
            ? `\n${summary.skipped} lines were skipped.`
            : "";
        const errorNote =
          summary.errors.length > 0
            ? `\nErrors:\n- ${summary.errors.join("\n- ")}`
            : "";
        window.alert(`${base}${skippedNote}${errorNote}`.trim());
      };
      reader.onerror = () => {
        window.alert(
          `Import for ${label} failed: ${
            reader.error?.message ?? "Unknown error"
          }`
        );
      };
      reader.readAsText(file);
    },
    []
  );

  const handleImportActionInstancesFile = useCallback(
    (file: File) =>
      handleImportFromFile(
        file,
        importActionInstancesFromText,
        "Action Instances"
      ),
    [handleImportFromFile, importActionInstancesFromText]
  );

  const handleExportCanvasGraph = useCallback(() => {
    const payload: ExportedCanvasGraphV2 = {
      version: 2,
      exportedAt: new Date().toISOString(),
      graph,
      separators,
    };

    const json = JSON.stringify(payload, null, 2);
    const blob = new Blob([json], { type: "application/json" });
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = "aptree-canvas.json";
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();

    URL.revokeObjectURL(url);
  }, [graph, separators]);

  const handleImportCanvasGraphFile = useCallback(
    async (file: File) => {
      try {
        const text = await file.text();
        const parsed: unknown = JSON.parse(text);

        const isCanvasGraph = (value: unknown): value is CanvasGraph => {
          if (!value || typeof value !== "object") {
            return false;
          }

          const graph = value as CanvasGraph;
          return Array.isArray(graph.nodes) && Array.isArray(graph.connections);
        };

        const isCanvasSeparator = (value: unknown): value is CanvasSeparator => {
          if (!value || typeof value !== "object") {
            return false;
          }

          const obj = value as Partial<CanvasSeparator>;
          return (
            typeof obj.id === "string" &&
            typeof obj.y === "number" &&
            Number.isFinite(obj.y)
          );
        };

        const isV2 = (value: unknown): value is ExportedCanvasGraphV2 => {
          if (!value || typeof value !== "object") {
            return false;
          }

          const obj = value as Partial<ExportedCanvasGraphV2>;
          if (obj.version !== 2) {
            return false;
          }

          if (!isCanvasGraph(obj.graph)) {
            return false;
          }

          if (!Array.isArray(obj.separators)) {
            return false;
          }

          return obj.separators.every(isCanvasSeparator);
        };

        const isV1 = (value: unknown): value is ExportedCanvasGraphsV1 => {
          if (!value || typeof value !== "object") {
            return false;
          }

          const obj = value as Partial<ExportedCanvasGraphsV1>;
          if (obj.version !== 1) {
            return false;
          }

          const isCanvasLevel = (lvl: unknown): lvl is "high" | "mid" | "low" =>
            lvl === "high" || lvl === "mid" || lvl === "low";

          if (!isCanvasLevel(obj.activeLevel)) {
            return false;
          }
          if (!obj.graphs || typeof obj.graphs !== "object") {
            return false;
          }

          const graphsObj = obj.graphs as Record<string, unknown>;
          return (
            isCanvasGraph(graphsObj.high) &&
            isCanvasGraph(graphsObj.mid) &&
            isCanvasGraph(graphsObj.low)
          );
        };

        const migrateV1GraphsToV2 = (
          graphs: Record<"high" | "mid" | "low", CanvasGraph>
        ): { graph: CanvasGraph; separators: CanvasSeparator[] } => {
          const levels: Array<"high" | "mid" | "low"> = ["high", "mid", "low"];

          const nextNodes: CanvasNode[] = [];
          const nextConnections: NodeConnection[] = [];
          const nextSeparators: CanvasSeparator[] = [];

          let cursorY = 0;
          const betweenLevelGap = 220;
          const topPadding = 120;
          const headerOffset = 60;

          for (const level of levels) {
            const levelGraph = graphs[level];
            if (!levelGraph.nodes.length && !levelGraph.connections.length) {
              continue;
            }

            const nodes = levelGraph.nodes;

            // compute vertical bounds in the original coordinate space
            let minY = 0;
            let maxY = 0;
            if (nodes.length) {
              minY = Infinity;
              maxY = -Infinity;
              for (const node of nodes) {
                const h = node.height ?? DEFAULT_CANVAS_NODE_HEIGHT;
                minY = Math.min(minY, node.y - h / 2);
                maxY = Math.max(maxY, node.y + h / 2);
              }
            }

            if (!Number.isFinite(minY) || !Number.isFinite(maxY)) {
              minY = 0;
              maxY = 0;
            }

            const shiftY = cursorY - minY + topPadding;
            const separatorY = cursorY + topPadding - headerOffset;
            nextSeparators.push({ id: createId("separator"), y: separatorY });

            nextNodes.push(
              ...nodes.map((node) => ({
                ...node,
                y: node.y + shiftY,
              }))
            );

            nextConnections.push(
              ...levelGraph.connections.map((conn) => ({
                ...conn,
                id: `${level}-${conn.id}`,
              }))
            );

            cursorY = maxY + shiftY + betweenLevelGap;
          }

          return {
            graph: {
              nodes: nextNodes,
              connections: nextConnections,
              rootNodeId: null,
            },
            separators: nextSeparators,
          };
        };

        if (isV2(parsed)) {
          pushHistory("import-graph");
          setGraph(normalizeImportedCanvasGraph(parsed.graph));
          setSeparators(normalizeImportedSeparators(parsed.separators));
          setParameterDetail(null);
          return;
        }

        if (isV1(parsed)) {
          const migrated = migrateV1GraphsToV2(parsed.graphs);
          pushHistory("import-graph");
          setGraph(normalizeImportedCanvasGraph(migrated.graph));
          setSeparators(normalizeImportedSeparators(migrated.separators));
          setParameterDetail(null);
          return;
        }

        // lenient fallback: allow importing a raw v2-like object
        if (parsed && typeof parsed === "object") {
          const obj = parsed as Record<string, unknown>;
          const candidateGraph = obj.graph && typeof obj.graph === "object" ? obj.graph : obj;

          if (isCanvasGraph(candidateGraph)) {
            const candidateSeparators =
              Array.isArray(obj.separators) && obj.separators.every(isCanvasSeparator)
                ? (obj.separators as CanvasSeparator[])
                : [];

            pushHistory("import-graph");
            setGraph(normalizeImportedCanvasGraph(candidateGraph as CanvasGraph));
            setSeparators(normalizeImportedSeparators(candidateSeparators));
            setParameterDetail(null);
            return;
          }
        }

        window.alert(
          "Import failed: JSON did not match the expected canvas format (v2 graph + separators, or legacy v1 graphs)."
        );
      } catch (error) {
        window.alert(
          `Import failed: ${error instanceof Error ? error.message : "Unknown error"}`
        );
      }
    },
    [pushHistory]
  );

  const handleImportAptreeModelFile = useCallback(
    async (file: File) => {
      try {
        const modelText = await file.text();

        const response = await fetch("/api/aptree/validate", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ modelText, instancesText: null, jarPath: null }),
        });

        const responseText = await response.text();
        const json: unknown = (() => {
          try {
            return JSON.parse(responseText);
          } catch {
            return null;
          }
        })();

        const parsed = normalizeAptreeValidateResponse(json);

        const extraLogs: string[] = [];
        if (parsed.findings && parsed.findings.length > 0) {
          extraLogs.push("Findings:", ...parsed.findings.map((line) => `- ${line}`));
        }
        if (parsed.stderr) {
          extraLogs.push("Stderr:", parsed.stderr);
        }
        if (parsed.toolLogs) {
          extraLogs.push("Tool logs:", parsed.toolLogs);
        }

        if (!response.ok) {
          const errors = parsed.errors?.length
            ? parsed.errors
            : [`HTTP ${response.status}`];
          const details = extraLogs.length ? extraLogs.join("\n") : "";
          console.error("APTree import failed", {
            status: response.status,
            errors,
            details,
            responseText,
            parsed,
          });
          window.alert("Parsing error. See console for details.");
          return;
        }

        if (parsed.ok === false) {
          const errors = parsed.errors?.length
            ? parsed.errors
            : ["Model validation failed"];
          const details = extraLogs.length ? extraLogs.join("\n") : "";
          console.error("APTree validation failed", {
            errors,
            details,
            responseText,
            parsed,
          });
          window.alert("Parsing error. See console for details.");
          return;
        }

        const candidateGraphs =
          parsed.graphs && parsed.graphs.length > 0
            ? parsed.graphs
            : parsed.graph
              ? [parsed.graph]
              : [];

        const hasAnyNodes = candidateGraphs.some((candidate) => candidate.nodes.length > 0);
        if (candidateGraphs.length === 0 || !hasAnyNodes) {
          window.alert(
            "APTree import succeeded, but no graph data was returned by the tool. Build the MontiCore tool jar with: (cd APTreeDSL && gradle shadowJar)."
          );
          return;
        }

        const importResult =
          candidateGraphs.length > 1
            ? aptreeGraphsToCanvasGraph(candidateGraphs, actionTypes ?? [])
            : aptreeGraphToCanvasGraph(candidateGraphs[0]!, actionTypes ?? []);

        // Add discovered action types to the sidebar
        if (importResult.discoveredActionTypes.length > 0) {
          addActionTypes(importResult.discoveredActionTypes);
        }

        // Add discovered action instances to the sidebar
        if (importResult.discoveredActionInstances.length > 0) {
          addActionInstances(importResult.discoveredActionInstances);
        }

        pushHistory("import-aptree");
        setGraph(importResult.graph);
        setSeparators([]);
        setParameterDetail(null);
      } catch (error) {
        window.alert(
          `APTree import failed: ${error instanceof Error ? error.message : "Unknown error"}`
        );
      }
    },
    [actionTypes, addActionTypes, addActionInstances, pushHistory]
  );

  /**
   * opens the appropriate sidebar edit modal for the node's source item, if available.
   */
  const handleEditNodeFromCanvas = useCallback(
    (nodeId: string) => {
      const node = graph.nodes.find((entry) => entry.id === nodeId);
      if (!node) {
        console.warn("Unable to edit node; node not found", nodeId);
        return;
      }

      const category = node.category;
      const items = getItemsForCategory(category);
      const index = items.findIndex((item) => item.id === node.sourceId);

      if (index === -1) {
        window.alert(
          "This element cannot currently be edited via the canvas. Please edit it in the sidebar."
        );
        console.warn(
          "Unable to edit node; no matching source item found",
          node
        );
        return;
      }

      const item = items[index];
      openEditModal(category, index, item);
    },
    [getItemsForCategory, graph.nodes, openEditModal]
  );

  /**
   * handles dropping a sidebar item onto the editor canvas.
   */
  const handleDropOnCanvas = useCallback(
    (item: DraggedSidebarItem, position: { x: number; y: number }) => {
      const isAction =
        item.kind === "actionType" || item.kind === "actionInstance";

      if (item.category === BT_NODES_KEY) {
        const option = behaviorNodeOptionMap.get(item.id);

        if (option) {
          pushHistory("drop-node");
          setGraph((prev) => ({
            ...prev,
            nodes: [...prev.nodes, createBehaviorNode({ option, position })],
          }));
          return;
        }
      }

      pushHistory("drop-node");
      setGraph((prev) => ({
        ...prev,
        nodes: [
          ...prev.nodes,
          {
            id: createId("canvas-node"),
            sourceId: item.id,
            name: item.name,
            typeLabel: item.type,
            category: item.category,
            kind: item.kind,
            x: position.x,
            y: position.y,
            width: DEFAULT_CANVAS_NODE_WIDTH,
            height: DEFAULT_CANVAS_NODE_HEIGHT,
            isNegated: item.isNegated,
            typeId: item.typeId,
            preconditions: isAction ? [] : undefined,
            effects: isAction ? [] : undefined,
          },
        ],
      }));
    },
    [behaviorNodeOptionMap, pushHistory]
  );

  /**
   * Sets a flow node as the single root node.
   */
  const handleSetRootNode = useCallback((nodeId: string) => {
    pushHistory("set-root");
    setGraph((prev) => {
      if (prev.rootNodeId === nodeId) {
        return { ...prev, rootNodeId: null };
      }

      const node = prev.nodes.find((entry) => entry.id === nodeId);
      if (!node) {
        return prev;
      }

      if (node.category !== FLOW_NODES_KEY) {
        window.alert("Only Flow nodes can be set as the root.");
        return prev;
      }

      return { ...prev, rootNodeId: nodeId };
    });
  }, [pushHistory]);

  const handleDropSeparator = useCallback(
    (position: { x: number; y: number }) => {
      pushHistory("drop-separator");
      setSeparators((prev) => [
        ...prev,
        {
          id: createId("separator"),
          y: position.y,
        },
      ]);
    },
    [pushHistory]
  );

  const handleBeginMoveSeparator = useCallback(
    () => {
      pushHistory("move-separator");
    },
    [pushHistory]
  );

  const handleMoveSeparator = useCallback((separatorId: string, y: number) => {
    setSeparators((prev) =>
      prev.map((separator) =>
        separator.id === separatorId ? { ...separator, y } : separator
      )
    );
  }, []);

  const handleRemoveSeparator = useCallback((separatorId: string) => {
    pushHistory("remove-separator");
    setSeparators((prev) => prev.filter((separator) => separator.id !== separatorId));
  }, [pushHistory]);

  /**
   * handles moving an existing node within the editor canvas.
   */
  const handleMoveNode = useCallback(
    (nodeId: string, position: { x: number; y: number }) => {
      setGraph((prev) => ({
        ...prev,
        nodes: prev.nodes.map((node) =>
          node.id === nodeId
            ? {
                ...node,
                x: position.x,
                y: position.y,
              }
            : node
        ),
      }));
    },
    []
  );

  const handleBeginMoveNode = useCallback(
    () => {
      pushHistory("move-node");
    },
    [pushHistory]
  );

  /**
   * persists resize interactions emitted from the canvas.
   */
  const handleResizeNode = useCallback(
    (nodeId: string, size: { width: number; height: number }) => {
      pushHistory("resize-node");
      setGraph((prev) => ({
        ...prev,
        nodes: prev.nodes.map((node) =>
          node.id === nodeId
            ? {
                ...node,
                width: Math.max(120, size.width),
                height: Math.max(100, size.height),
              }
            : node
        ),
      }));
    },
    [pushHistory]
  );

  /**
   * handles removing a node from the editor canvas.
   */
  const handleRemoveNode = useCallback((nodeId: string) => {
    pushHistory("remove-node");
    setGraph((prev) => ({
      ...prev,
      nodes: prev.nodes.filter((node) => node.id !== nodeId),
      connections: prev.connections.filter(
        (conn) => conn.sourceNodeId !== nodeId && conn.targetNodeId !== nodeId
      ),
      rootNodeId: prev.rootNodeId === nodeId ? null : prev.rootNodeId,
    }));
    setPredicateModalState((prev) =>
      prev && prev.nodeId === nodeId ? null : prev
    );
  }, [pushHistory]);

  const handleAddPredicate = useCallback(
    (nodeId: string, group: PredicateGroup, predicate: PredicateInstance) => {
      pushHistory("add-predicate");
      setGraph((prev) => ({
        ...prev,
        nodes: prev.nodes.map((node) => {
          if (node.id !== nodeId) {
            return node;
          }

          const key = group === "precondition" ? "preconditions" : "effects";
          const existing = node[key] ?? [];
          return {
            ...node,
            [key]: [...existing, predicate],
          };
        }),
      }));
    },
    [pushHistory]
  );

  const handleRemovePredicate = useCallback(
    (nodeId: string, group: PredicateGroup, predicateId: string) => {
      pushHistory("remove-predicate");
      setGraph((prev) => ({
        ...prev,
        nodes: prev.nodes.map((node) => {
          if (node.id !== nodeId) {
            return node;
          }

          const key = group === "precondition" ? "preconditions" : "effects";
          const existing = node[key] ?? [];
          return {
            ...node,
            [key]: existing.filter((entry) => entry.id !== predicateId),
          };
        }),
      }));
    },
    [pushHistory]
  );


  /**
   * handles adding a connection between two nodes.
   */
  const handleAddConnection = useCallback(
    (
      sourceNodeId: string,
      targetNodeId: string,
      sourcePort: "top" | "right" | "bottom" | "left",
      targetPort: "top" | "right" | "bottom" | "left"
    ) => {
      // Check if connection already exists
      setGraph((prev) => {
        const exists = prev.connections.some(
          (conn) =>
            conn.sourceNodeId === sourceNodeId &&
            conn.targetNodeId === targetNodeId &&
            conn.sourcePort === sourcePort &&
            conn.targetPort === targetPort
        );

        if (exists) {
          return prev;
        }

        pushHistory("add-connection");
        return {
          ...prev,
          connections: [
            ...prev.connections,
            {
              id: createId("connection"),
              sourceNodeId,
              targetNodeId,
              sourcePort,
              targetPort,
            },
          ],
        };
      });
    },
    [pushHistory]
  );

  /**
   * handles removing a connection between nodes.
   */
  const handleRemoveConnection = useCallback((connectionId: string) => {
    pushHistory("remove-connection");
    setGraph((prev) => ({
      ...prev,
      connections: prev.connections.filter((conn) => conn.id !== connectionId),
    }));
  }, [pushHistory]);

  /**
   * handles cycling the flow success type for a flow node.
   */
  const handleCycleFlowSuccessType = useCallback((nodeId: string) => {
    pushHistory("cycle-success");
    setGraph((prev) => ({
      ...prev,
      nodes: prev.nodes.map((node) => {
        if (node.id !== nodeId || !node.successType) {
          return node;
        }

        const currentIndex = Math.max(0, FLOW_SUCCESS_TYPES.indexOf(node.successType));
        const nextType =
          FLOW_SUCCESS_TYPES[(currentIndex + 1) % FLOW_SUCCESS_TYPES.length];

        return {
          ...node,
          successType: nextType,
        };
      }),
    }));
  }, [pushHistory]);

  /**
   * handles creating a new behavior node on the canvas.
   */
  const handleCreateBehaviorNode = useCallback((option: BehaviorNodeOption) => {
    pushHistory("create-node");
    setGraph((prev) => {
      const nextIndex = prev.nodes.length;
      const offset = 140;
      const position = {
        x: 140 + (nextIndex % 3) * offset,
        y: 140 + Math.floor(nextIndex / 3) * offset,
      };

      return {
        ...prev,
        nodes: [...prev.nodes, createBehaviorNode({ option, position })],
      };
    });
  }, [pushHistory]);

  return (
    <>
      <div className="app-container">
        <Sidebar
          manager={sidebarManager}
          onCreateBehaviorNode={handleCreateBehaviorNode}
        />
        <div className="main-content">
          <Header
            theme={theme}
            onToggleTheme={handleToggleTheme}
            onUndo={handleUndo}
            onRedo={handleRedo}
            canUndo={undoStack.length > 0}
            canRedo={redoStack.length > 0}
            onImportActionInstances={handleImportActionInstancesFile}
            onExportCanvasGraph={handleExportCanvasGraph}
            onImportCanvasGraph={handleImportCanvasGraphFile}
            onImportAptreeModel={handleImportAptreeModelFile}
            onOpenValidate={() => setIsValidateOpen(true)}
          />
          <div className="editor" role="main">
            <div className="editor-canvas-wrap">
              <EditorCanvas
                nodes={graph.nodes}
                separators={separators}
                connections={graph.connections}
                rootNodeId={graph.rootNodeId}
                onDropNode={handleDropOnCanvas}
                onBeginMoveNode={handleBeginMoveNode}
                onBeginMoveSeparator={handleBeginMoveSeparator}
                onDropSeparator={handleDropSeparator}
                onMoveNode={handleMoveNode}
                onMoveSeparator={handleMoveSeparator}
                onResizeNode={handleResizeNode}
                onRemoveNode={handleRemoveNode}
                onRemoveSeparator={handleRemoveSeparator}
                onEditNode={handleEditNodeFromCanvas}
                onAddConnection={handleAddConnection}
                onRemoveConnection={handleRemoveConnection}
                onShowActionParameterDetail={handleShowActionParameterDetail}
                onCycleFlowSuccessType={handleCycleFlowSuccessType}
                onSetRootNode={handleSetRootNode}
                onOpenPredicateModal={handleOpenPredicateModal}
                actionTypes={actionTypes}
                actionInstances={actionInstances}
                tickStatus={tickStatus}
              />
            </div>
          </div>
        </div>
      </div>

      <ActionParameterDetailsModal
        detail={parameterDetail}
        onClose={handleCloseActionParameterDetail}
      />

      {predicateModalState ? (
        <ActionPredicateModal
          isOpen
          node={
            graph.nodes.find((entry) => entry.id === predicateModalState.nodeId) ??
            null
          }
          predicateTypes={predicateTypes}
          activeGroup={predicateModalState.group}
          onChangeGroup={handleChangePredicateGroup}
          onClose={handleClosePredicateModal}
          onAddPredicate={handleAddPredicate}
          onRemovePredicate={handleRemovePredicate}
      />
      ) : null}

      <AptreeValidateModal
        isOpen={isValidateOpen}
        onClose={() => setIsValidateOpen(false)}
      />
    </>
  );
}

export default App;
