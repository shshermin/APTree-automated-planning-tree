import type { DragEvent } from "react";
import type {
  ActionInstance,
  ActionType,
  CategoryItemListProps,
  DataCategory,
  ParameterType,
  PredicateType,
  StructuredItem,
} from "../utils/types";
import {
  ACTION_INSTANCES_KEY,
  ACTION_TYPES_KEY,
  BT_NODES_KEY,
  PARAM_TYPES_KEY,
  PREDICATE_TYPES_KEY,
  DRAGGABLE_NODE_CATEGORIES,
} from "../utils/constants";
import {
  DRAG_DATA_FORMAT,
  resolveDragEntityKind,
  type DraggedSidebarItem,
} from "../../editor/dragTypes";

type TypeLookup = {
  actionTypeMap: Map<string, ActionType>;
};

const includesQuery = (value: string, query: string) =>
  value.toLowerCase().includes(query);

const matchesTypeDefinition = (typeItem: ParameterType, query: string) =>
  typeItem.properties.some((property) =>
    includesQuery(`${property.name} ${property.valueType}`, query)
  );

  /**
   * collects property strings for matching against the search query.
   * @param typeDefinition 
   * @param propertyValues 
   * @returns array of concatenated property name/value strings
   */
const collectPropertyStrings = (
  typeDefinition: ParameterType | PredicateType | ActionType | undefined,
  propertyValues: Record<string, string> | undefined
) => {
  if (!typeDefinition || !propertyValues) {
    return [];
  }

  return typeDefinition.properties.map((property) => {
    const label = property.name ?? "";
    const value = propertyValues[property.id] ?? "";
    return `${label} ${value}`.trim();
  });
};

/**
 * matches an action instance against the search query.
 * @param instance 
 * @param query 
 * @param actionTypeMap 
 * @returns true when a match is found
 */
const matchesActionInstance = (
  instance: ActionInstance,
  query: string,
  actionTypeMap: Map<string, ActionType>
) => {
  const linkedType = actionTypeMap.get(instance.typeId);
  const typeName = (linkedType?.name ?? instance.type).toLowerCase();
  if (typeName.includes(query)) {
    return true;
  }

  return collectPropertyStrings(linkedType, instance.propertyValues).some((entry) =>
    includesQuery(entry, query)
  );
};

/**
 * checks if an item matches the search query for the given category.
 * @param category active category used to determine additional fields to inspect
 * @param query normalized search query string
 * @param item structured item currently being evaluated
 * @param lookups aggregated type lookup tables for instances
 * @returns true when the item contains the query in its main fields or nested data
 */
const matchesSearch = (
  category: DataCategory,
  query: string,
  item: StructuredItem,
  lookups: TypeLookup
): boolean => {
  if (!query) {
    return true;
  }

  const lowerQuery = query.toLowerCase();
  const base = `${item.name} ${item.type}`.toLowerCase();
  if (base.includes(lowerQuery)) {
    return true;
  }

  switch (category) {
    case PARAM_TYPES_KEY:
    case PREDICATE_TYPES_KEY:
    case ACTION_TYPES_KEY:
      return matchesTypeDefinition(item as ParameterType, lowerQuery);
    case ACTION_INSTANCES_KEY:
      return matchesActionInstance(
        item as ActionInstance,
        lowerQuery,
        lookups.actionTypeMap
      );
    default:
      return false;
  }
};

/** presentation details for a sidebar item. */
interface ItemPresentation {
  badgeLabel: string;
  dragTypeId?: string;
  dragIsNegated?: boolean;
}

/**
 * resolves the presentation details for a sidebar item based on its category.
 * @param category 
 * @param item 
 * @param lookups 
 * @returns 
 */
const resolveItemPresentation = (
  category: DataCategory,
  item: StructuredItem,
  lookups: TypeLookup
): ItemPresentation => {
  if (category === ACTION_INSTANCES_KEY) {
    const instance = item as ActionInstance;
    const linkedType = lookups.actionTypeMap.get(instance.typeId);
    return {
      badgeLabel: linkedType?.name ?? item.type,
      dragTypeId: instance.typeId,
    };
  }

  return { badgeLabel: item.type };
};

/**
 * builds the drag payload for a sidebar item based on its category and presentation.
 * @param category 
 * @param item 
 * @param presentation 
 * @returns drag payload or null when the category is not draggable
 */
const buildDragPayload = (
  category: DataCategory,
  item: StructuredItem,
  presentation: ItemPresentation
): DraggedSidebarItem | null => {
  if (!DRAGGABLE_NODE_CATEGORIES.includes(category)) {
    return null;
  }

  const payload: DraggedSidebarItem = {
    id: item.id,
    name: item.name,
    type: presentation.badgeLabel,
    category,
    kind: resolveDragEntityKind(category),
  };

  if (presentation.dragTypeId) {
    payload.typeId = presentation.dragTypeId;
  }

  if (typeof presentation.dragIsNegated === "boolean") {
    payload.isNegated = presentation.dragIsNegated;
  }

  return payload;
};

/**
 * resolves the appropriate empty-state message based on context.
 * @param category 
 * @param trimmedQuery 
 * @param parameterTypes 
 * @param predicateTypes 
 * @param actionTypes 
 * @param isActionCategory 
 * @param isBehaviorNodeCategory 
 * @returns contextual empty-state message
 */
const resolveEmptyStateMessage = (
  category: DataCategory,
  trimmedQuery: string,
  actionTypes: ActionType[],
  isActionCategory: boolean,
  isBehaviorNodeCategory: boolean
) => {
  if (trimmedQuery) {
    return "No matching items.";
  }

  if (category === ACTION_INSTANCES_KEY && actionTypes.length === 0) {
    return "Create an action type before adding instances.";
  }

  if (isActionCategory) {
    return "Use the BT node wizard to add or configure action entries.";
  }

  if (isBehaviorNodeCategory) {
    return "Use the Add Behavior Node wizard to create new flow, decorator, or service nodes.";
  }

  return "No items defined.";
};

/**
 * renders the filtered list of items inside a sidebar section and exposes edit/delete affordances.
 * @param props composite props for the rendered list and callbacks
 * @returns sidebar list markup including empty-state messaging
 */
export function CategoryItemList({
  category,
  items,
  actionTypes,
  actionTypeMap,
  searchQuery,
  onEdit,
  onDelete,
  readOnly = false,
}: CategoryItemListProps) {
  const trimmedQuery = searchQuery.trim();
  const isActionInstanceCategory = category === ACTION_INSTANCES_KEY;
  const isActionTypeCategory = category === ACTION_TYPES_KEY;
  const isActionCategory = isActionInstanceCategory || isActionTypeCategory;
  const isBehaviorNodeCategory = category === BT_NODES_KEY;
  const isDraggableCategory = DRAGGABLE_NODE_CATEGORIES.includes(category);
  const lookups: TypeLookup = {
    actionTypeMap,
  };

  const filteredItems = trimmedQuery
    ? items.filter((item) =>
        matchesSearch(category, trimmedQuery, item, lookups)
      )
    : items;

  return (
    <div className="item-list-container">
      {filteredItems.map((item, index) => {
        const presentation = resolveItemPresentation(category, item, lookups);
        const dragPayload = isDraggableCategory
          ? buildDragPayload(category, item, presentation)
          : null;

          /**
           * handles the drag start event for a sidebar item.
           * @param event drag event  
           */
        const handleDragStart = (event: DragEvent<HTMLSpanElement>) => {
          if (!dragPayload) {
            return;
          }

          const serializedPayload = JSON.stringify(dragPayload);
          event.dataTransfer.setData("text/plain", item.name);
          event.dataTransfer.setData(DRAG_DATA_FORMAT, serializedPayload);
          event.dataTransfer.effectAllowed = "copy";
        };

        /**
         * handles the drag end event for a sidebar item.
         * @param event drag event 
         */
        const handleDragEnd = (event: DragEvent<HTMLSpanElement>) => {
          if (!dragPayload) {
            return;
          }

          const dataTransfer = event.dataTransfer;
          if (dataTransfer) {
            dataTransfer.clearData(DRAG_DATA_FORMAT);
          }
        };

        return (
          <div
            key={item.id || `${item.name}-${index}`}
            className="list-item-box"
          >
            <span
              className="list-item-text"
              draggable={isDraggableCategory}
              onDragStart={handleDragStart}
              onDragEnd={handleDragEnd}
              data-drag-kind={dragPayload?.kind}
            >
              <span className="item-name">{item.name}</span>
              <span className="item-meta">
                <span className="list-item-separator" aria-hidden>
                  :
                </span>
                <span className="type-badge">
                  <span className="type-badge-primary">
                    {presentation.badgeLabel}
                  </span>
                </span>
              </span>
            </span>

            {!readOnly && (
              <div className="list-item-actions">
                <button
                  className="icon-btn edit-btn"
                  onClick={() => onEdit(category, index, item)}
                  title="Edit"
                  type="button"
                >
                  ✎
                </button>
                <button
                  className="icon-btn delete-btn"
                  onClick={() => onDelete(category, index)}
                  title="Delete"
                  type="button"
                >
                  ×
                </button>
              </div>
            )}
          </div>
        );
      })}

      {filteredItems.length === 0 && (
        <p className="empty-copy">
          {resolveEmptyStateMessage(
            category,
            trimmedQuery,
            actionTypes,
            isActionCategory,
            isBehaviorNodeCategory
          )}
        </p>
      )}
    </div>
  );
}
