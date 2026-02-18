import type {
  ActionType,
  ImportReport,
  ParameterType,
  PredicateType,
} from "./types";

export type PropertyBackedType = ParameterType | PredicateType | ActionType;

export interface ParsedAssignments {
  named: Record<string, string>;
  ordered: string[];
}

const stripQuotes = (value: string) => value.replace(/^['"]|['"]$/g, "");

/**
 * parses a string block containing property assignments into named and ordered collections.
 * @param block input string block with property assignments
 * @returns parsed assignments object
 */
export const parseAssignmentBlock = (block: string): ParsedAssignments => {
  const named: Record<string, string> = {};
  const ordered: string[] = [];
  const entries = block
    .split(",")
    .map((segment) => segment.trim())
    .filter(Boolean);

  entries.forEach((entry) => {
    const match = entry.match(/^([A-Za-z0-9_]+)\s*[:=]\s*(.+)$/);
    if (match) {
      const key = match[1].trim().toLowerCase();
      const value = stripQuotes(match[2].trim());
      named[key] = value;
    } else {
      ordered.push(stripQuotes(entry));
    }
  });

  return { named, ordered };
};

/**
 * builds a property values mapping from the provided type definition and parsed assignments.
 * @param definition 
 * @param assignment  
 * @returns record mapping property IDs to their assigned values
 */
export const buildPropertyValuesFromAssignments = (
  definition: PropertyBackedType,
  assignments: ParsedAssignments
): Record<string, string> => {
  if (!definition.properties.length) {
    return {};
  }

  const fallbackValues = [...assignments.ordered];
  return definition.properties.reduce<Record<string, string>>((acc, property) => {
    const normalizedKey = property.name.trim().toLowerCase();
    if (normalizedKey && assignments.named[normalizedKey] !== undefined) {
      acc[property.id] = assignments.named[normalizedKey];
    } else if (fallbackValues.length > 0) {
      acc[property.id] = fallbackValues.shift() ?? "";
    } else {
      acc[property.id] = "";
    }
    return acc;
  }, {});
};

/**
 * picks a display name for an instance based on the provided type name and parsed assignments.
 * @param typeName 
 * @param assignments  
 * @returns chosen display name for the instance
 */
export const pickInstanceDisplayName = (
  typeName: string,
  assignments: ParsedAssignments
) => {
  if (assignments.named["name"]) {
    return assignments.named["name"];
  }

  if (assignments.named["id"]) {
    return assignments.named["id"];
  }

  if (assignments.ordered[0]) {
    return assignments.ordered[0];
  }

  const firstNamedKey = Object.keys(assignments.named)[0];
  if (firstNamedKey) {
    return assignments.named[firstNamedKey];
  }

  return `${typeName}-${Math.random().toString(36).slice(2, 6)}`;
};

/**
 * summarizes the results of an import operation.
 * @param processed 
 * @param imported 
 * @param errors 
 * @returns import report object
 */
export const summarizeImport = (
  processed: number,
  imported: number,
  errors: string[]
): ImportReport => ({
  processed,
  imported,
  skipped: Math.max(processed - imported, 0),
  errors,
});
