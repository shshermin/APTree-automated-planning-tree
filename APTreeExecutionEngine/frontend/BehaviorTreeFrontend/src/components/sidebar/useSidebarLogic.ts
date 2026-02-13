import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ADD_LABELS,
  DEFAULT_DATA,
  DEFAULT_ORDER,
  DEFAULT_TITLES,
  DECORATOR_NODES_KEY,
  ACTION_INSTANCES_KEY,
  ACTION_TYPES_KEY,
  FALLBACK_FLOW_NODE_OPTIONS,
  FALLBACK_DECORATOR_NODE_OPTIONS,
  FALLBACK_NODEGRAPH_NODE_OPTIONS,
  PARAM_TYPES_KEY,
  PREDICATE_TYPES_KEY,
  SERVICE_NODES_KEY,
  FALLBACK_SERVICE_NODE_OPTIONS,
} from "./utils/constants";
import {
  cloneActionInstance,
  cloneActionType,
  cloneParameterType,
  clonePredicateType,
  createEmptyActionInstance,
  createEmptyActionType,
  createEmptyParameterType,
  createEmptyPredicateType,
  createEmptyStructuredItem,
  generateItemId,
  generatePropertyId,
  reconcileInstanceValues,
} from "./utils/helpers";
import type {
  ActionInstance,
  ActionType,
  AppData,
  CategoryModalState,
  DataCategory,
  ModalState,
  FlowNodeOption,
  NodeGraphNodeOption,
  ParameterType,
  PredicateType,
  SearchQueries,
  SidebarManager,
  StructuredItem,
  DecoratorNodeOption,
  ServiceNodeOption,
  ImportReport,
} from "./utils/types";
import { useModalController } from "./utils/modalController";
import {
  buildPropertyValuesFromAssignments,
  parseAssignmentBlock,
  pickInstanceDisplayName,
  summarizeImport,
} from "./utils/importParsing";

/**
 * normalizes type definitions by trimming fields and ensuring property ids.
 * @param value type definition being persisted
 * @param createEmpty factory providing a fallback structure for missing ids
 * @returns sanitized type definition ready for storage
 */
const normalizeType = (
  value: ParameterType,
  createEmpty: () => ParameterType
): ParameterType => ({
  ...value,
  id: value.id || createEmpty().id,
  name: value.name.trim(),
  type: value.type.trim(),
  properties: value.properties.map((property) => ({
    ...property,
    id: property.id || generatePropertyId(),
    name: property.name.trim(),
    valueType: property.valueType.trim(),
  })),
});

/**
 * builds the initial search-query map with empty strings per default category.
 * @returns initialized search query map keyed by category
 */
const createInitialSearchQueries = (): SearchQueries => {
  const initialEntries = DEFAULT_ORDER.map((key) => [key, ""] as const);
  return Object.fromEntries(initialEntries) as SearchQueries;
};

/**
 * central hook that encapsulates sidebar state, modal coordination, and item crud helpers.
 * @returns api for interacting with sidebar state and derived data
 */
export const useSidebarManager = (): SidebarManager => {
  const [data, setData] = useState<AppData>(() => ({ ...DEFAULT_DATA }));
  const [categoryTitles, setCategoryTitles] = useState<Record<string, string>>(
    () => ({ ...DEFAULT_TITLES })
  );
  const [categoryOrder, setCategoryOrder] = useState<string[]>(() => [
    ...DEFAULT_ORDER,
  ]);
  const [searchQueries, setSearchQueries] = useState<SearchQueries>(
    createInitialSearchQueries
  );
  const [flowCatalog, setFlowCatalog] = useState<StructuredItem[] | null>(null);

  const [categoryModalState, setCategoryModalState] =
    useState<CategoryModalState>({
      isOpen: false,
      mode: "add",
      activeKey: null,
      value: createEmptyStructuredItem(),
    });

  const parameterTypeModal = useModalController<ParameterType>(
    createEmptyParameterType
  );
  const predicateTypeModal = useModalController<PredicateType>(
    createEmptyPredicateType
  );
  const actionTypeModal = useModalController<ActionType>(
    createEmptyActionType
  );
  const actionInstanceModal = useModalController<ActionInstance>(
    createEmptyActionInstance
  );

  const parameterTypeModalState = parameterTypeModal.state;
  const predicateTypeModalState = predicateTypeModal.state;
  const actionTypeModalState = actionTypeModal.state;
  const actionInstanceModalState = actionInstanceModal.state;

  const [modalState, setModalState] = useState<ModalState>(() => ({
    isOpen: false,
    mode: "add",
    category: null,
    index: null,
    initialValue: createEmptyStructuredItem(),
  }));

  const parameterTypes = useMemo(() => {
    const entries = data[PARAM_TYPES_KEY] as ParameterType[] | undefined;
    return entries ? [...entries] : [];
  }, [data]);

  const predicateTypes = useMemo(() => {
    const entries = data[PREDICATE_TYPES_KEY] as PredicateType[] | undefined;
    return entries ? [...entries] : [];
  }, [data]);

  const actionTypes = useMemo(() => {
    const entries = data[ACTION_TYPES_KEY] as ActionType[] | undefined;
    return entries ? [...entries] : [];
  }, [data]);

  const parameterTypeMap = useMemo(() => {
    const map = new Map<string, ParameterType>();
    parameterTypes.forEach((entry) => {
      map.set(entry.id, entry);
    });
    return map;
  }, [parameterTypes]);

  const predicateTypeMap = useMemo(() => {
    const map = new Map<string, PredicateType>();
    predicateTypes.forEach((entry) => {
      map.set(entry.id, entry);
    });
    return map;
  }, [predicateTypes]);

  const actionTypeMap = useMemo(() => {
    const map = new Map<string, ActionType>();
    actionTypes.forEach((entry) => {
      map.set(entry.id, entry);
    });
    return map;
  }, [actionTypes]);

  const actionTypeNameMap = useMemo(() => {
    const map = new Map<string, ActionType>();
    actionTypes.forEach((entry) => {
      map.set(entry.name.trim().toLowerCase(), entry);
    });
    return map;
  }, [actionTypes]);

  const needsDecoratorCatalog = useMemo(() => {
    const entries = data[DECORATOR_NODES_KEY] as StructuredItem[] | undefined;
    return !entries || entries.length === 0;
  }, [data]);

  const needsServiceCatalog = useMemo(() => {
    const entries = data[SERVICE_NODES_KEY] as StructuredItem[] | undefined;
    return !entries || entries.length === 0;
  }, [data]);

  useEffect(() => {
    if (!needsDecoratorCatalog && !needsServiceCatalog) {
      return;
    }

    const controller = new AbortController();

    const toStructuredItems = (entries: Array<{ id: string; label: string; typeLabel?: string | null; description?: string | null }>): StructuredItem[] =>
      entries.map((entry) => ({
        id: entry.id,
        name: entry.label,
        type: entry.typeLabel ?? "",
        description: entry.description ?? "",
      }));

    const load = async () => {
      try {
        const [decorators, services] = await Promise.all([
          needsDecoratorCatalog
            ? fetch("/api/catalog/decorators", { signal: controller.signal }).then((res) => {
                if (!res.ok) {
                  throw new Error(`Decorator catalog request failed (${res.status})`);
                }
                return res.json() as Promise<
                  Array<{ id: string; label: string; typeLabel?: string | null; kind?: string; description?: string | null }>
                >;
              })
            : Promise.resolve(null),
          needsServiceCatalog
            ? fetch("/api/catalog/services", { signal: controller.signal }).then((res) => {
                if (!res.ok) {
                  throw new Error(`Service catalog request failed (${res.status})`);
                }
                return res.json() as Promise<
                  Array<{ id: string; label: string; typeLabel?: string | null; kind?: string; description?: string | null }>
                >;
              })
            : Promise.resolve(null),
        ]);

        setData((prev) => {
          const next = { ...prev };

          const existingDecorators = next[DECORATOR_NODES_KEY] as StructuredItem[] | undefined;
          if (needsDecoratorCatalog && (!existingDecorators || existingDecorators.length === 0)) {
            next[DECORATOR_NODES_KEY] = decorators
              ? toStructuredItems(decorators)
              : FALLBACK_DECORATOR_NODE_OPTIONS.map((option) => ({
                  id: option.id,
                  name: option.label,
                  type: option.typeLabel,
                  description: option.description ?? "",
                }));
          }

          const existingServices = next[SERVICE_NODES_KEY] as StructuredItem[] | undefined;
          if (needsServiceCatalog && (!existingServices || existingServices.length === 0)) {
            next[SERVICE_NODES_KEY] = services
              ? toStructuredItems(services)
              : FALLBACK_SERVICE_NODE_OPTIONS.map((option) => ({
                  id: option.id,
                  name: option.label,
                  type: option.typeLabel,
                  description: option.description ?? "",
                }));
          }

          return next;
        });
      } catch (error) {
        if (controller.signal.aborted) {
          return;
        }
        console.warn("Failed to load backend node catalogs; using frontend defaults.", error);
        setData((prev) => {
          const next = { ...prev };
          const existingDecorators = next[DECORATOR_NODES_KEY] as StructuredItem[] | undefined;
          const existingServices = next[SERVICE_NODES_KEY] as StructuredItem[] | undefined;

          if (needsDecoratorCatalog && (!existingDecorators || existingDecorators.length === 0)) {
            next[DECORATOR_NODES_KEY] = FALLBACK_DECORATOR_NODE_OPTIONS.map((option) => ({
              id: option.id,
              name: option.label,
              type: option.typeLabel,
              description: option.description ?? "",
            }));
          }

          if (needsServiceCatalog && (!existingServices || existingServices.length === 0)) {
            next[SERVICE_NODES_KEY] = FALLBACK_SERVICE_NODE_OPTIONS.map((option) => ({
              id: option.id,
              name: option.label,
              type: option.typeLabel,
              description: option.description ?? "",
            }));
          }

          return next;
        });
      }
    };

    void load();

    return () => controller.abort();
  }, [needsDecoratorCatalog, needsServiceCatalog]);

  useEffect(() => {
    const controller = new AbortController();

    const toStructuredItems = (
      entries: Array<{ id: string; label: string; typeLabel?: string | null; description?: string | null }>
    ): StructuredItem[] =>
      entries.map((entry) => ({
        id: entry.id,
        name: entry.label,
        type: entry.typeLabel ?? "",
        description: entry.description ?? "",
      }));

    const load = async () => {
      try {
        const flows = await fetch("/api/catalog/flows", { signal: controller.signal }).then((res) => {
          if (!res.ok) {
            throw new Error(`Flow catalog request failed (${res.status})`);
          }
          return res.json() as Promise<
            Array<{ id: string; label: string; typeLabel?: string | null; kind?: string; description?: string | null }>
          >;
        });

        setFlowCatalog(toStructuredItems(flows));
      } catch (error) {
        if (controller.signal.aborted) {
          return;
        }

        console.warn("Failed to load backend flow catalog; using frontend defaults.", error);
        setFlowCatalog(
          FALLBACK_FLOW_NODE_OPTIONS.map((option) => ({
            id: option.id,
            name: option.label,
            type: option.typeLabel,
            description: option.description ?? "",
          }))
        );
      }
    };

    void load();
    return () => controller.abort();
  }, []);

  const decoratorNodeOptions = useMemo<DecoratorNodeOption[]>(() => {
    const entries = data[DECORATOR_NODES_KEY] as StructuredItem[] | undefined;
    const source =
      entries ??
      FALLBACK_DECORATOR_NODE_OPTIONS.map((option) => ({
        id: option.id,
        name: option.label,
        type: option.typeLabel,
        description: option.description ?? "",
      }));

    return source.map((item) => ({
      id: item.id,
      label: item.name,
      typeLabel: item.type || "Decorator",
      description: item.description || undefined,
      kind: "decorator",
    }));
  }, [data]);

  const serviceNodeOptions = useMemo<ServiceNodeOption[]>(() => {
    const entries = data[SERVICE_NODES_KEY] as StructuredItem[] | undefined;
    const source =
      entries ??
      FALLBACK_SERVICE_NODE_OPTIONS.map((option) => ({
        id: option.id,
        name: option.label,
        type: option.typeLabel,
        description: option.description ?? "",
      }));

    return source.map((item) => ({
      id: item.id,
      label: item.name,
      typeLabel: item.type || "Service",
      description: item.description || undefined,
      kind: "service",
    }));
  }, [data]);

  const flowNodeOptions = useMemo<FlowNodeOption[]>(() => {
    const source =
      flowCatalog ??
      FALLBACK_FLOW_NODE_OPTIONS.map((option) => ({
        id: option.id,
        name: option.label,
        type: option.typeLabel,
        description: option.description ?? "",
      }));

    return source.map((item) => ({
      id: item.id,
      label: item.name,
      typeLabel: item.type || "Flow",
      description: item.description || undefined,
      kind: "flow",
      defaultSuccessType: "ALL",
    }));
  }, [flowCatalog]);

  const nodeGraphNodeOptions = useMemo<NodeGraphNodeOption[]>(
    () => FALLBACK_NODEGRAPH_NODE_OPTIONS.map((option) => ({ ...option })),
    []
  );

  /**
   * Opens the "add item" modal for the given category.
   * Special handling for typed categories.
   *
   * @param category The category for which to add a new item.
   */
  const openInstanceModalWithDefault = <TType, TInstance>(
    definitions: TType[],
    createEmpty: (type?: TType) => TInstance,
    openModal: (initialValue?: TInstance) => void,
    emptyMessage: string
  ) => {
    if (definitions.length === 0) {
      window.alert(emptyMessage);
      return;
    }

    const defaultDefinition = definitions[0];
    openModal(createEmpty(defaultDefinition));
  };

  /**
   * opens the generic item modal in add mode for the specified category.
   * @param category category key representing the data section 
   */
  const openAddModal = (category: DataCategory) => {
    if (category === PARAM_TYPES_KEY) {
      parameterTypeModal.openAdd();
      return;
    }

    if (category === PREDICATE_TYPES_KEY) {
      predicateTypeModal.openAdd();
      return;
    }

    if (category === ACTION_TYPES_KEY) {
      actionTypeModal.openAdd();
      return;
    }

    if (category === ACTION_INSTANCES_KEY) {
      openInstanceModalWithDefault(
        actionTypes,
        createEmptyActionInstance,
        actionInstanceModal.openAdd,
        "Create an action type before adding instances."
      );
      return;
    }

    if (category === DECORATOR_NODES_KEY || category === SERVICE_NODES_KEY) {
      const defaultTypeLabel =
        category === DECORATOR_NODES_KEY ? "Decorator" : "Service";

      setModalState({
        isOpen: true,
        mode: "add",
        category,
        index: null,
        initialValue: {
          id: generateItemId(),
          name: "",
          type: defaultTypeLabel,
          description: "",
        },
      });
      return;
    }

    setModalState({
      isOpen: true,
      mode: "add",
      category,
      index: null,
      initialValue: createEmptyStructuredItem(),
    });
  };

  /**
   * opens the parameter type modal in edit mode with a cloned payload.
   * @param index index of the parameter type within the list
   * @param currentValue parameter type currently selected for editing
   */
  const openEditParameterType = (
    index: number,
    currentValue: ParameterType
  ) => {
    parameterTypeModal.openEdit(index, cloneParameterType(currentValue));
  };

  /**
   * opens the predicate type modal in edit mode with a cloned payload.
   * @param index index of the predicate type within the list
   * @param currentValue predicate type currently selected for editing
   */
  const openEditPredicateType = (
    index: number,
    currentValue: PredicateType
  ) => {
    predicateTypeModal.openEdit(index, clonePredicateType(currentValue));
  };

  /**
   * opens the action type modal in edit mode with a cloned payload.
   * @param index index of the action type within the list
   * @param currentValue action type currently selected for editing
   */
  const openEditActionType = (index: number, currentValue: ActionType) => {
    actionTypeModal.openEdit(index, cloneActionType(currentValue));
  };

  /**
   * opens the action instance modal in edit mode with reconciled values.
   * @param index index of the action instance within the list
   * @param currentValue instance currently selected for editing
   */
  const openEditActionInstance = (
    index: number,
    currentValue: ActionInstance
  ) => {
    const actionType = actionTypeMap.get(currentValue.typeId);
    const initialEntry = cloneActionInstance(currentValue);

    if (actionType) {
      initialEntry.type = actionType.name;
      initialEntry.propertyValues = reconcileInstanceValues(
        actionType,
        currentValue.propertyValues
      );
    }

    actionInstanceModal.openEdit(index, initialEntry);
  };

  /**
   * Opens the edit modal for a generic structured item in a category.
   * Delegates to type-specific modals for typed categories.
   */
  const openEditModal = (
    category: DataCategory,
    index: number,
    currentValue: StructuredItem
  ) => {
    if (category === PARAM_TYPES_KEY) {
      openEditParameterType(index, currentValue as ParameterType);
      return;
    }

    if (category === PREDICATE_TYPES_KEY) {
      openEditPredicateType(index, currentValue as PredicateType);
      return;
    }

    if (category === ACTION_TYPES_KEY) {
      openEditActionType(index, currentValue as ActionType);
      return;
    }

    if (category === ACTION_INSTANCES_KEY) {
      openEditActionInstance(index, currentValue as ActionInstance);
      return;
    }

    setModalState({
      isOpen: true,
      mode: "edit",
      category,
      index,
      initialValue: {
        ...currentValue,
        description: currentValue.description ?? "",
      },
    });
  };

  /**
   * Closes the generic item modal without saving.
   */
  const closeModal = () => {
    setModalState((prev) => ({ ...prev, isOpen: false }));
  };

  /**
   * Saves a new or edited structured item from the generic modal.
   */
  const handleSaveFromModal = (value: StructuredItem) => {
    const categoryKey = modalState.category;
    if (!categoryKey) return;

    setData((prev) => {
      const existingItems = prev[categoryKey] ?? [];
      const nextItems = [...existingItems];
      const fallbackTypeLabel =
        categoryKey === DECORATOR_NODES_KEY
          ? "Decorator"
          : categoryKey === SERVICE_NODES_KEY
          ? "Service"
          : value.type;
      const normalized: StructuredItem = {
        ...value,
        id: value.id || generateItemId(),
        name: value.name.trim(),
        type: (value.type || "").trim() || fallbackTypeLabel,
        description: value.description?.trim() || undefined,
      };

      if (modalState.mode === "add") {
        nextItems.push(normalized);
      } else if (modalState.mode === "edit" && modalState.index !== null) {
        nextItems[modalState.index] = normalized;
      }

      return { ...prev, [categoryKey]: nextItems };
    });

    closeModal();
  };

  /**
   * closes the parameter type modal and restores its default state.
   */
  const closeParameterTypeModal = () => {
    parameterTypeModal.close();
  };

  /**
   * closes the predicate type modal and restores its default state.
   */
  const closePredicateTypeModal = () => {
    predicateTypeModal.close();
  };

  /**
   * closes the action type modal and restores its default state.
   */
  const closeActionTypeModal = () => {
    actionTypeModal.close();
  };

  /**
   * closes the action instance modal and restores its default state.
   */
  const closeActionInstanceModal = () => {
    actionInstanceModal.close();
  };

  /**
   * persists the provided parameter type and reconciles linked instances.
   * @param value parameter type collected from the modal form
   */
  const handleSaveParameterType = (value: ParameterType) => {
    const normalized = normalizeType(value, createEmptyParameterType);

    setData((prev) => {
      const existingTypes =
        (prev[PARAM_TYPES_KEY] as ParameterType[] | undefined) ?? [];
      const nextTypes = [...existingTypes];

      if (parameterTypeModalState.mode === "add") {
        nextTypes.push(normalized);
      } else if (
        parameterTypeModalState.mode === "edit" &&
        parameterTypeModalState.index !== null
      ) {
        nextTypes[parameterTypeModalState.index] = normalized;
      }

      return {
        ...prev,
        [PARAM_TYPES_KEY]: nextTypes,
      };
    });

    closeParameterTypeModal();
  };

  /**
   * persists the provided predicate type and reconciles linked instances.
   * @param value predicate type collected from the modal form
   */
  const handleSavePredicateType = (value: PredicateType) => {
    const normalized = {
      ...normalizeType(value, createEmptyPredicateType),
      type: "predicate",
    } as PredicateType;

    setData((prev) => {
      const existingTypes =
        (prev[PREDICATE_TYPES_KEY] as PredicateType[] | undefined) ?? [];
      const nextTypes = [...existingTypes];

      if (predicateTypeModalState.mode === "add") {
        nextTypes.push(normalized);
      } else if (
        predicateTypeModalState.mode === "edit" &&
        predicateTypeModalState.index !== null
      ) {
        nextTypes[predicateTypeModalState.index] = normalized;
      }

      return {
        ...prev,
        [PREDICATE_TYPES_KEY]: nextTypes,
      };
    });

    closePredicateTypeModal();
  };

  /**
   * persists the provided action type and reconciles linked instances.
   * @param value action type collected from the modal form
   */
  const handleSaveActionType = (value: ActionType) => {
    const normalized = {
      ...normalizeType(value, createEmptyActionType),
      type: "GenericBTAction",
    } as ActionType;

    setData((prev) => {
      const existingTypes =
        (prev[ACTION_TYPES_KEY] as ActionType[] | undefined) ?? [];
      const nextTypes = [...existingTypes];

      if (actionTypeModalState.mode === "add") {
        nextTypes.push(normalized);
      } else if (
        actionTypeModalState.mode === "edit" &&
        actionTypeModalState.index !== null
      ) {
        nextTypes[actionTypeModalState.index] = normalized;
      }

      const existingInstances =
        (prev[ACTION_INSTANCES_KEY] as ActionInstance[] | undefined) ?? [];
      const nextInstances = existingInstances.map((instance) => {
        if (instance.typeId !== normalized.id) {
          return instance;
        }

        return {
          ...instance,
          type: normalized.name,
          propertyValues: reconcileInstanceValues(
            normalized,
            instance.propertyValues
          ),
        };
      });

      return {
        ...prev,
        [ACTION_TYPES_KEY]: nextTypes,
        [ACTION_INSTANCES_KEY]: nextInstances,
      };
    });

    closeActionTypeModal();
  };

  /**
   * stores the provided action instance after validating its type binding.
   * @param value action instance captured from the modal form
   */
  const handleSaveActionInstance = (value: ActionInstance) => {
    const actionType = actionTypeMap.get(value.typeId);
    if (!actionType) {
      window.alert("Select a valid action type.");
      return;
    }

    const sanitizedValues = actionType.properties.reduce<
      Record<string, string>
    >((acc, property) => {
      const rawValue = value.propertyValues?.[property.id] ?? "";
      acc[property.id] = rawValue.trim();
      return acc;
    }, {});

    const normalized: ActionInstance = {
      ...value,
      id: value.id || createEmptyActionInstance().id,
      name: value.name.trim(),
      type: actionType.name,
      typeId: actionType.id,
      propertyValues: sanitizedValues,
    };

    setData((prev) => {
      const existing =
        (prev[ACTION_INSTANCES_KEY] as ActionInstance[] | undefined) ?? [];
      const next = [...existing];

      if (actionInstanceModalState.mode === "add") {
        next.push(normalized);
      } else if (
        actionInstanceModalState.mode === "edit" &&
        actionInstanceModalState.index !== null
      ) {
        next[actionInstanceModalState.index] = normalized;
      }

      return {
        ...prev,
        [ACTION_INSTANCES_KEY]: next,
      };
    });

    closeActionInstanceModal();
  };

  /**
   * removes an item from the requested category and cleans up dependent state.
   * @param category category key hosting the targeted item
   * @param index position of the item inside the category list
   */
  const handleDeleteItem = (category: DataCategory, index: number) => {
    if (!window.confirm("Delete this item?")) {
      return;
    }

    const removedEntry = (data[category] ?? [])[index];

    setData((prev) => {
      const existingItems = prev[category] ?? [];
      const nextItems = existingItems.filter((_, i) => i !== index);
      const nextData: AppData = { ...prev, [category]: nextItems };

      if (category === ACTION_TYPES_KEY && removedEntry) {
        const removedType = removedEntry as ActionType;
        const existingInstances =
          (prev[ACTION_INSTANCES_KEY] as ActionInstance[] | undefined) ?? [];
        nextData[ACTION_INSTANCES_KEY] = existingInstances.filter(
          (instance) => instance.typeId !== removedType.id
        );
      }

      return nextData;
    });

    const closeModalEditingIndex = (
      targetCategory: DataCategory,
      modalState: { isOpen: boolean; index: number | null },
      close: () => void
    ) => {
      if (category === targetCategory && modalState.isOpen && modalState.index === index) {
        close();
      }
    };

    const closeInstanceModalIfOrphaned = (
      typeCategory: DataCategory,
      removedType: StructuredItem | undefined,
      modalState: { isOpen: boolean; initialValue: { typeId: string } },
      close: () => void
    ) => {
      if (
        category === typeCategory &&
        removedType &&
        modalState.isOpen &&
        modalState.initialValue.typeId === removedType.id
      ) {
        close();
      }
    };

    closeModalEditingIndex(
      ACTION_INSTANCES_KEY,
      actionInstanceModalState,
      closeActionInstanceModal
    );
    closeModalEditingIndex(
      PARAM_TYPES_KEY,
      parameterTypeModalState,
      closeParameterTypeModal
    );
    closeModalEditingIndex(
      PREDICATE_TYPES_KEY,
      predicateTypeModalState,
      closePredicateTypeModal
    );
    closeModalEditingIndex(
      ACTION_TYPES_KEY,
      actionTypeModalState,
      closeActionTypeModal
    );

    closeInstanceModalIfOrphaned(
      ACTION_TYPES_KEY,
      removedEntry,
      actionInstanceModalState,
      closeActionInstanceModal
    );
  };

  /**
   * opens the category modal in add mode with a blank payload.
   */
  const openCategoryModal = () => {
    setCategoryModalState({
      isOpen: true,
      mode: "add",
      activeKey: null,
      value: createEmptyStructuredItem(),
    });
  };

  /**
   * opens the category modal prefilled for renaming the provided key.
   * @param categoryKey identifier of the category being renamed
   */
  const openRenameCategoryModal = (categoryKey: DataCategory) => {
    const currentTitle = categoryTitles[categoryKey] ?? categoryKey;
    setCategoryModalState({
      isOpen: true,
      mode: "edit",
      activeKey: categoryKey,
      value: {
        ...createEmptyStructuredItem(),
        name: currentTitle,
      },
    });
  };

  /**
   * closes the category modal and restores its default payload.
   */
  const closeCategoryModal = () => {
    setCategoryModalState({
      isOpen: false,
      mode: "add",
      activeKey: null,
      value: createEmptyStructuredItem(),
    });
  };

  /**
   * transforms a category label into a unique slug identifier.
   * @param label human readable category label supplied by the user
   * @returns slugified category key guaranteed to be unique
   */
  const createCategoryKey = (label: string) => {
    const baseKey = label
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "");

    const candidateBase = baseKey || "category";
    let candidate = candidateBase;
    let suffix = 1;
    const existingKeys = new Set([...categoryOrder, ...Object.keys(data)]);

    while (existingKeys.has(candidate)) {
      candidate = `${candidateBase}-${suffix++}`;
    }

    return candidate;
  };

  /**
   * persists category changes either by creating a new section or renaming.
   * @param value structured payload captured from the category modal
   */
  const handleSaveCategory = (value: StructuredItem) => {
    const displayName = value.name.trim();
    if (!displayName) {
      return;
    }

    if (categoryModalState.mode === "add") {
      const newKey = createCategoryKey(displayName);
      setData((prev) => ({ ...prev, [newKey]: [] }));
      setCategoryTitles((prev) => ({ ...prev, [newKey]: displayName }));
      setCategoryOrder((prev) => [...prev, newKey]);
      setSearchQueries((prev) => ({ ...prev, [newKey]: "" }));
    } else if (
      categoryModalState.mode === "edit" &&
      categoryModalState.activeKey
    ) {
      const activeKey = categoryModalState.activeKey;
      setCategoryTitles((prev) => ({
        ...prev,
        [activeKey]: displayName,
      }));
    }

    closeCategoryModal();
  };

  /**
   * removes an entire category and clears all nested data and modals.
   * @param categoryKey identifier of the category to remove
   */
  const handleDeleteCategory = (categoryKey: DataCategory) => {
    const displayName = categoryTitles[categoryKey] ?? categoryKey;
    if (
      !window.confirm(`Delete section "${displayName}" and all of its items?`)
    ) {
      return;
    }

    setCategoryOrder((prev) => prev.filter((key) => key !== categoryKey));
    setCategoryTitles((prev) => {
      const nextTitles = { ...prev };
      delete nextTitles[categoryKey];
      return nextTitles;
    });
    setData((prev) => {
      const nextData: AppData = { ...prev };
      delete nextData[categoryKey];
      return nextData;
    });
    setSearchQueries((prev) => {
      const nextQueries = { ...prev };
      delete nextQueries[categoryKey];
      return nextQueries;
    });
    setModalState((prev) =>
      prev.category === categoryKey
        ? {
            isOpen: false,
            mode: "add",
            category: null,
            index: null,
            initialValue: createEmptyStructuredItem(),
          }
        : prev
    );

    if (categoryKey === PARAM_TYPES_KEY) {
      closeParameterTypeModal();
    }

    if (categoryKey === PREDICATE_TYPES_KEY) {
      closePredicateTypeModal();
    }

    if (categoryKey === ACTION_TYPES_KEY) {
      closeActionTypeModal();
    }

    if (categoryKey === ACTION_INSTANCES_KEY) {
      closeActionInstanceModal();
    }

    if (categoryModalState.activeKey === categoryKey) {
      closeCategoryModal();
    }
  };

  /**
   * retrieves the items for a given category with a memoized lookup.
   * @param category category key used to look up stored items
   * @returns array of structured items for the provided category
   */
  const getItemsForCategory = useCallback(
    (category: DataCategory) => data[category] ?? [],
    [data]
  );

  /**
   * updates the search query string for the specified category.
   * @param category category key whose search filter should update
   * @param value new search string entered by the user
   */
  const handleSearchChange = (category: DataCategory, value: string) => {
    setSearchQueries((prev) => ({ ...prev, [category]: value }));
  };

  /**
   * Imports action instances from the provided raw text input.
   * @param rawText multiline string containing action instance definitions
   * @returns summary report of the import operation
   */
  const importActionInstancesFromText = useCallback(
    (rawText: string): ImportReport => {
      const lines = rawText.split(/\r?\n/);
      const created: ActionInstance[] = [];
      const errors: string[] = [];
      let processed = 0;

      lines.forEach((line, index) => {
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith("#")) {
          return;
        }

        if (!trimmed.toLowerCase().startsWith("actioninstance")) {
          return;
        }

        processed += 1;
        const match = trimmed.match(
          /^ActionInstance:\s*([^\s(]+)\s*\(([^)]*)\)/i
        );
        if (!match) {
          errors.push(
            `Zeile ${index + 1}: Ungültiges ActionInstance-Format.`
          );
          return;
        }

        const typeName = match[1].trim();
        const definition = actionTypeNameMap.get(typeName.toLowerCase());
        if (!definition) {
          errors.push(
            `Zeile ${index + 1}: Unbekannter Action-Typ "${typeName}".`
          );
          return;
        }

        const assignments = parseAssignmentBlock(match[2].trim());
        const instance = createEmptyActionInstance(definition);
        instance.name = pickInstanceDisplayName(definition.name, assignments);
        instance.propertyValues = buildPropertyValuesFromAssignments(
          definition,
          assignments
        );
        created.push(instance);
      });

      if (created.length > 0) {
        setData((prev) => {
          const existing =
            (prev[ACTION_INSTANCES_KEY] as ActionInstance[] | undefined) ?? [];
          return {
            ...prev,
            [ACTION_INSTANCES_KEY]: [...existing, ...created],
          };
        });
      }

      return summarizeImport(processed, created.length, errors);
    },
    [actionTypeNameMap, setData]
  );

  /**
   * resolves the localized add button label for the supplied category key.
   * @param category category key whose label should be retrieved
   * @returns translated add button label
   */
  const addLabelFor = (category: DataCategory) =>
    ADD_LABELS[category] ?? "Add Item";

  return {
    addLabelFor,
    categoryModal: categoryModalState,
    categoryOrder,
    categoryTitles,
    closeCategoryModal,
    closeActionInstanceModal,
    closeModal,
    closeParameterTypeModal,
    closePredicateTypeModal,
    closeActionTypeModal,
    getItemsForCategory,
    handleDeleteCategory,
    handleDeleteItem,
    handleSaveCategory,
    handleSaveFromModal,
    handleSaveActionInstance,
    handleSaveParameterType,
    handleSavePredicateType,
    handleSaveActionType,
    handleSearchChange,
    actionInstanceModalState,
    modalState,
    openAddModal,
    openCategoryModal,
    openEditModal,
    openRenameCategoryModal,
    parameterTypeMap,
    parameterTypes,
    predicateTypeMap,
    predicateTypes,
    actionTypeMap,
    actionTypes,
    flowNodeOptions,
    decoratorNodeOptions,
    serviceNodeOptions,
    nodeGraphNodeOptions,
    searchQueries,
    parameterTypeModalState,
    predicateTypeModalState,
    actionTypeModalState,
    importActionInstancesFromText,
  };
};