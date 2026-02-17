import type { PredicateType } from "../components/sidebar/utils/types";

const createProperty = (id: string, name: string, valueType: string) => ({
  id,
  name,
  valueType,
});

/**
 * canonical predicate-type definitions derived from the backend models.
 * keeps the frontend forms aligned with the C# predicate signatures.
 */
export const PREDICATE_TYPE_CATALOG: PredicateType[] = [
  {
    id: "predicate-type-allset",
    name: "allset",
    type: "predicate",
    properties: [
      createProperty("property-allset-lay", "lay", "Layer"),
      createProperty("property-allset-mod", "mod", "Module"),
    ],
  },
  {
    id: "predicate-type-atagent",
    name: "atAgent",
    type: "predicate",
    properties: [
      createProperty("property-atagent-agent", "agent", "Agent"),
      createProperty("property-atagent-location", "location", "Location"),
    ],
  },
  {
    id: "predicate-type-atplace",
    name: "atplace",
    type: "predicate",
    properties: [
      createProperty("property-atplace-myObject", "myObject", "Element"),
      createProperty("property-atplace-place", "place", "Location"),
    ],
  },
  {
    id: "predicate-type-belongstolayer",
    name: "belongstolayer",
    type: "predicate",
    properties: [
      createProperty("property-belongstolayer-myObject", "myObject", "Element"),
      createProperty("property-belongstolayer-lay", "lay", "Layer"),
    ],
  },
  {
    id: "predicate-type-belongstomodule",
    name: "belongstomodule",
    type: "predicate",
    properties: [
      createProperty("property-belongstomodule-myObject", "myObject", "Element"),
      createProperty("property-belongstomodule-mod", "mod", "Module"),
    ],
  },
  {
    id: "predicate-type-clear",
    name: "clear",
    type: "predicate",
    properties: [createProperty("property-clear-myObject", "myObject", "Element")],
  },
  {
    id: "predicate-type-empty",
    name: "empty",
    type: "predicate",
    properties: [createProperty("property-empty-client", "client", "Agent")],
  },
  {
    id: "predicate-type-glued",
    name: "glued",
    type: "predicate",
    properties: [createProperty("property-glued-myObject", "myObject", "Element")],
  },
  {
    id: "predicate-type-hastool",
    name: "hasTool",
    type: "predicate",
    properties: [
      createProperty("property-hastool-agent", "agent", "Agent"),
      createProperty("property-hastool-tool", "tool", "Tool"),
    ],
  },
  {
    id: "predicate-type-holding",
    name: "holding",
    type: "predicate",
    properties: [
      createProperty("property-holding-agent", "agent", "Agent"),
      createProperty("property-holding-myObject", "myObject", "Element"),
    ],
  },
  {
    id: "predicate-type-isat",
    name: "isAt",
    type: "predicate",
    properties: [
      createProperty("property-isat-myObject", "myObject", "Element"),
      createProperty("property-isat-location", "location", "Location"),
    ],
  },
  {
    id: "predicate-type-nailed",
    name: "nailed",
    type: "predicate",
    properties: [createProperty("property-nailed-myObject", "myObject", "Element")],
  },
  {
    id: "predicate-type-ontop",
    name: "ontop",
    type: "predicate",
    properties: [
      createProperty("property-ontop-myObject1", "myObject1", "Element"),
      createProperty("property-ontop-myObject2", "myObject2", "Element"),
    ],
  },
  {
    id: "predicate-type-positionfree",
    name: "positionfree",
    type: "predicate",
    properties: [createProperty("property-positionfree-pos", "pos", "Location")],
  },
  {
    id: "predicate-type-stacked",
    name: "stacked",
    type: "predicate",
    properties: [createProperty("property-stacked-myObject", "myObject", "Element")],
  },
];
