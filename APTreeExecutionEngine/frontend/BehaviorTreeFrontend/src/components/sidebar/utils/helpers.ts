import { createId } from "../../../utils/id";
import type {
  ActionInstance,
  ActionType,
  ParameterType,
  PredicateType,
  StructuredItem,
} from "./types";

/**
 * generates a unique identifier for generic structured items.
 * @returns freshly generated item id
 */
export const generateItemId = () => createId("item");

/**
 * creates an empty structured item scaffold.
 * @returns blank structured item ready for user input
 */
export const createEmptyStructuredItem = (): StructuredItem => ({
  id: generateItemId(),
  name: "",
  type: "",
  description: "",
});

/**
 * creates an empty parameter-type definition with no properties.
 * @returns blank parameter type descriptor
 */
export const createEmptyParameterType = (): ParameterType => ({
  ...createEmptyStructuredItem(),
  id: createId("param-type"),
  properties: [],
});

/**
 * creates an empty predicate-type definition with no properties.
 * @returns blank predicate type descriptor
 */
export const createEmptyPredicateType = (): PredicateType => ({
  ...createEmptyStructuredItem(),
  id: createId("predicate-type"),
  type: "predicate",
  properties: [],
});

/**
 * creates an empty action-type definition with no properties.
 * @returns blank action type descriptor
 */
export const createEmptyActionType = (): ActionType => ({
  ...createEmptyStructuredItem(),
  id: createId("action-type"),
  type: "GenericBTAction",
  properties: [],
});

/**
 * deep clones a parameter type including its properties.
 * @param entry parameter type that should be cloned
 * @returns cloned parameter type
 */
export const cloneParameterType = (entry: ParameterType): ParameterType => ({
  ...entry,
  properties: entry.properties.map((property) => ({ ...property })),
});

/**
 * deep clones a predicate type including its properties.
 * @param entry predicate type that should be cloned
 * @returns cloned predicate type
 */
export const clonePredicateType = (entry: PredicateType): PredicateType => ({
  ...entry,
  type: entry.type || "predicate",
  properties: entry.properties.map((property) => ({ ...property })),
});

/**
 * deep clones an action type including its properties.
 * @param entry action type that should be cloned
 * @returns cloned action type
 */
export const cloneActionType = (entry: ActionType): ActionType => ({
  ...entry,
  properties: entry.properties.map((property) => ({ ...property })),
});

/**
 * creates an empty action instance optionally seeded from an action type.
 * @param actionType optional type describing the expected action structure
 * @returns blank action instance structure
 */
export const createEmptyActionInstance = (
  actionType?: ActionType
): ActionInstance => ({
  ...createEmptyStructuredItem(),
  id: createId("action-instance"),
  type: actionType?.name ?? "",
  typeId: actionType?.id ?? "",
  propertyValues: actionType
    ? actionType.properties.reduce<Record<string, string>>(
        (acc, property) => {
          acc[property.id] = "";
          return acc;
        },
        {}
      )
    : {},
});

/**
 * deep clones an action instance including its property values map.
 * @param entry action instance to clone
 * @returns cloned action instance
 */
export const cloneActionInstance = (entry: ActionInstance): ActionInstance => ({
  ...entry,
  propertyValues: { ...entry.propertyValues },
});

/**
 * aligns stored typed-instance values with the latest type schema.
 * @param definition type describing the expected property ids
 * @param currentValues map containing the current property values
 * @returns reconciled property value map containing all expected keys
 */
export const reconcileInstanceValues = (
  definition: ParameterType | PredicateType | ActionType,
  currentValues: Record<string, string>
): Record<string, string> =>
  definition.properties.reduce<Record<string, string>>((acc, property) => {
    acc[property.id] = currentValues[property.id] ?? "";
    return acc;
  }, {});

/**
 * generates a unique identifier for a parameter property.
 * @returns freshly generated property id
 */
export const generatePropertyId = () => createId("property");
