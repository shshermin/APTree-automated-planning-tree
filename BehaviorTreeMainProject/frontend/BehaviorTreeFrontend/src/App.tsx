import { useCallback, useEffect, useMemo, useState } from "react";
import "./App.css";
import Header from "./components/header/Header.tsx";
import Sidebar from "./components/sidebar/Sidebar.tsx";
import { useSidebarManager } from "./components/sidebar/useSidebarLogic";
import EditorCanvas from "./components/editor/EditorCanvas.tsx";
import type {
  ActionParameterDetail,
  CanvasNode,
  NodeConnection,
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
} from "./components/sidebar/utils/constants";
import ActionParameterDetailsModal from "./components/editor/modals/ActionParameterDetailsModal.tsx";
import AptreeValidateModal from "./components/aptree/AptreeValidateModal";
import { FLOW_SUCCESS_TYPES } from "./components/sidebar/utils/types";
import type { ActionInstance, BehaviorNodeOption } from "./components/sidebar/utils/types";

type ThemeMode = "light" | "dark";

type CanvasLevel = "high" | "mid" | "low";

const CANVAS_LEVELS: Array<{ key: CanvasLevel; label: string }> = [
  { key: "high", label: "High" },
  { key: "mid", label: "Mid" },
  { key: "low", label: "Low" },
];

type CanvasGraph = {
  nodes: CanvasNode[];
  connections: NodeConnection[];
};

type ExportedCanvasGraphsV1 = {
  version: 1;
  exportedAt: string;
  activeLevel: CanvasLevel;
  graphs: Record<CanvasLevel, CanvasGraph>;
};

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
  const [theme, setTheme] = useState<ThemeMode>(getInitialTheme);
  const [userLockedTheme, setUserLockedTheme] = useState<boolean>(() => {
    if (typeof window === "undefined") {
      return false;
    }
    const savedTheme = window.localStorage.getItem(STORAGE_KEY);
    return savedTheme === "light" || savedTheme === "dark";
  });
  const [activeLevel, setActiveLevel] = useState<CanvasLevel>("high");
  const [graphs, setGraphs] = useState<Record<CanvasLevel, CanvasGraph>>(() => ({
    high: { nodes: [], connections: [] },
    mid: { nodes: [], connections: [] },
    low: { nodes: [], connections: [] },
  }));
  const [parameterDetail, setParameterDetail] =
    useState<ActionParameterDetail | null>(null);
  const [isValidateOpen, setIsValidateOpen] = useState(false);
  const sidebarManager = useSidebarManager();
  const {
    importActionInstancesFromText,
    actionTypes,
    getItemsForCategory,
    openEditModal,
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
    ];
    return new Map(options.map((option) => [option.id, option] as const));
  }, [
    sidebarManager.flowNodeOptions,
    sidebarManager.decoratorNodeOptions,
    sidebarManager.serviceNodeOptions,
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
    const payload: ExportedCanvasGraphsV1 = {
      version: 1,
      exportedAt: new Date().toISOString(),
      activeLevel,
      graphs,
    };

    const json = JSON.stringify(payload, null, 2);
    const blob = new Blob([json], { type: "application/json" });
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = "aptree-canvas-graphs.json";
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();

    URL.revokeObjectURL(url);
  }, [activeLevel, graphs]);

  const handleImportCanvasGraphFile = useCallback(
    async (file: File) => {
      try {
        const text = await file.text();
        const parsed: unknown = JSON.parse(text);

        const isCanvasLevel = (value: unknown): value is CanvasLevel =>
          value === "high" || value === "mid" || value === "low";

        const isCanvasGraph = (value: unknown): value is CanvasGraph => {
          if (!value || typeof value !== "object") {
            return false;
          }

          const graph = value as CanvasGraph;
          return Array.isArray(graph.nodes) && Array.isArray(graph.connections);
        };

        const isV1 = (value: unknown): value is ExportedCanvasGraphsV1 => {
          if (!value || typeof value !== "object") {
            return false;
          }

          const obj = value as Partial<ExportedCanvasGraphsV1>;
          if (obj.version !== 1) {
            return false;
          }
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

        let nextGraphs: Record<CanvasLevel, CanvasGraph> | null = null;
        let nextLevel: CanvasLevel | null = null;

        if (isV1(parsed)) {
          nextGraphs = parsed.graphs;
          nextLevel = parsed.activeLevel;
        } else if (parsed && typeof parsed === "object") {
          // lenient fallback: allow importing a raw graphs object
          const obj = parsed as Record<string, unknown>;
          const candidateGraphs = obj.graphs && typeof obj.graphs === "object" ? (obj.graphs as Record<string, unknown>) : obj;

          if (
            isCanvasGraph(candidateGraphs.high) &&
            isCanvasGraph(candidateGraphs.mid) &&
            isCanvasGraph(candidateGraphs.low)
          ) {
            nextGraphs = {
              high: candidateGraphs.high as CanvasGraph,
              mid: candidateGraphs.mid as CanvasGraph,
              low: candidateGraphs.low as CanvasGraph,
            };
          }

          if (isCanvasLevel(obj.activeLevel)) {
            nextLevel = obj.activeLevel;
          }
        }

        if (!nextGraphs) {
          window.alert(
            "Import failed: JSON did not match the expected canvas graph format."
          );
          return;
        }

        setGraphs(nextGraphs);
        if (nextLevel) {
          setActiveLevel(nextLevel);
        }

        setParameterDetail(null);
      } catch (error) {
        window.alert(
          `Import failed: ${error instanceof Error ? error.message : "Unknown error"}`
        );
      }
    },
    []
  );

  /**
   * opens the appropriate sidebar edit modal for the node's source item, if available.
   */
  const handleEditNodeFromCanvas = useCallback(
    (nodeId: string) => {
      const node = graphs[activeLevel].nodes.find((entry) => entry.id === nodeId);
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
    [activeLevel, getItemsForCategory, graphs, openEditModal]
  );

  /**
   * handles dropping a sidebar item onto the editor canvas.
   */
  const handleDropOnCanvas = useCallback(
    (item: DraggedSidebarItem, position: { x: number; y: number }) => {
      if (item.category === BT_NODES_KEY) {
        const option = behaviorNodeOptionMap.get(item.id);

        if (option) {
          setGraphs((prev) => {
            const graph = prev[activeLevel];
            return {
              ...prev,
              [activeLevel]: {
                ...graph,
                nodes: [...graph.nodes, createBehaviorNode({ option, position })],
              },
            };
          });
          return;
        }
      }

      setGraphs((prev) => {
        const graph = prev[activeLevel];
        return {
          ...prev,
          [activeLevel]: {
            ...graph,
            nodes: [
              ...graph.nodes,
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
              },
            ],
          },
        };
      });
    },
    [activeLevel, behaviorNodeOptionMap]
  );

  /**
   * handles moving an existing node within the editor canvas.
   */
  const handleMoveNode = useCallback(
    (nodeId: string, position: { x: number; y: number }) => {
      setGraphs((prev) => {
        const graph = prev[activeLevel];
        return {
          ...prev,
          [activeLevel]: {
            ...graph,
            nodes: graph.nodes.map((node) =>
              node.id === nodeId
                ? {
                    ...node,
                    x: position.x,
                    y: position.y,
                  }
                : node
            ),
          },
        };
      });
    },
    [activeLevel]
  );

  /**
   * persists resize interactions emitted from the canvas.
   */
  const handleResizeNode = useCallback(
    (nodeId: string, size: { width: number; height: number }) => {
      setGraphs((prev) => {
        const graph = prev[activeLevel];
        return {
          ...prev,
          [activeLevel]: {
            ...graph,
            nodes: graph.nodes.map((node) =>
              node.id === nodeId
                ? {
                    ...node,
                    width: Math.max(120, size.width),
                    height: Math.max(100, size.height),
                  }
                : node
            ),
          },
        };
      });
    },
    [activeLevel]
  );

  /**
   * handles removing a node from the editor canvas.
   */
  const handleRemoveNode = useCallback((nodeId: string) => {
    setGraphs((prev) => {
      const graph = prev[activeLevel];
      return {
        ...prev,
        [activeLevel]: {
          nodes: graph.nodes.filter((node) => node.id !== nodeId),
          connections: graph.connections.filter(
            (conn) =>
              conn.sourceNodeId !== nodeId && conn.targetNodeId !== nodeId
          ),
        },
      };
    });
  }, [activeLevel]);

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
      setGraphs((prev) => {
        const graph = prev[activeLevel];
        const exists = graph.connections.some(
          (conn) =>
            conn.sourceNodeId === sourceNodeId &&
            conn.targetNodeId === targetNodeId &&
            conn.sourcePort === sourcePort &&
            conn.targetPort === targetPort
        );

        if (exists) {
          return prev;
        }

        return {
          ...prev,
          [activeLevel]: {
            ...graph,
            connections: [
              ...graph.connections,
              {
                id: createId("connection"),
                sourceNodeId,
                targetNodeId,
                sourcePort,
                targetPort,
              },
            ],
          },
        };
      });
    },
    [activeLevel]
  );

  /**
   * handles removing a connection between nodes.
   */
  const handleRemoveConnection = useCallback((connectionId: string) => {
    setGraphs((prev) => {
      const graph = prev[activeLevel];
      return {
        ...prev,
        [activeLevel]: {
          ...graph,
          connections: graph.connections.filter((conn) => conn.id !== connectionId),
        },
      };
    });
  }, [activeLevel]);

  /**
   * handles cycling the flow success type for a flow node.
   */
  const handleCycleFlowSuccessType = useCallback((nodeId: string) => {
    setGraphs((prev) => {
      const graph = prev[activeLevel];
      return {
        ...prev,
        [activeLevel]: {
          ...graph,
          nodes: graph.nodes.map((node) => {
            if (node.id !== nodeId || !node.successType) {
              return node;
            }

            const currentIndex = Math.max(
              0,
              FLOW_SUCCESS_TYPES.indexOf(node.successType)
            );
            const nextType =
              FLOW_SUCCESS_TYPES[(currentIndex + 1) % FLOW_SUCCESS_TYPES.length];

            return {
              ...node,
              successType: nextType,
            };
          }),
        },
      };
    });
  }, [activeLevel]);

  /**
   * handles creating a new behavior node on the canvas.
   */
  const handleCreateBehaviorNode = useCallback((option: BehaviorNodeOption) => {
    setGraphs((prev) => {
      const graph = prev[activeLevel];
      const nextIndex = graph.nodes.length;
      const offset = 140;
      const position = {
        x: 140 + (nextIndex % 3) * offset,
        y: 140 + Math.floor(nextIndex / 3) * offset,
      };

      return {
        ...prev,
        [activeLevel]: {
          ...graph,
          nodes: [...graph.nodes, createBehaviorNode({ option, position })],
        },
      };
    });
  }, [activeLevel]);

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
            onImportActionInstances={handleImportActionInstancesFile}
            onExportCanvasGraph={handleExportCanvasGraph}
            onImportCanvasGraph={handleImportCanvasGraphFile}
            onOpenValidate={() => setIsValidateOpen(true)}
          />
          <div className="editor" role="main">
            <div className="level-tabs" role="tablist" aria-label="Canvas Level">
              {CANVAS_LEVELS.map((level) => (
                <button
                  key={level.key}
                  type="button"
                  className={`level-tab${
                    activeLevel === level.key ? " is-active" : ""
                  }`}
                  role="tab"
                  aria-selected={activeLevel === level.key}
                  onClick={() => setActiveLevel(level.key)}
                >
                  {level.label}
                </button>
              ))}
            </div>

            <div className="editor-canvas-wrap">
              <EditorCanvas
                nodes={graphs[activeLevel].nodes}
                connections={graphs[activeLevel].connections}
                onDropNode={handleDropOnCanvas}
                onMoveNode={handleMoveNode}
                onResizeNode={handleResizeNode}
                onRemoveNode={handleRemoveNode}
                onEditNode={handleEditNodeFromCanvas}
                onAddConnection={handleAddConnection}
                onRemoveConnection={handleRemoveConnection}
                onShowActionParameterDetail={handleShowActionParameterDetail}
                onCycleFlowSuccessType={handleCycleFlowSuccessType}
                actionTypes={actionTypes}
                actionInstances={actionInstances}
              />
            </div>
          </div>
        </div>
      </div>

      <ActionParameterDetailsModal
        detail={parameterDetail}
        onClose={handleCloseActionParameterDetail}
      />

      <AptreeValidateModal
        isOpen={isValidateOpen}
        onClose={() => setIsValidateOpen(false)}
      />
    </>
  );
}

export default App;
