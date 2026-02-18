import { useMemo, useState } from "react";
import type {
  CanvasNode,
  PredicateGroup,
  PredicateInstance,
} from "../types";
import type { PredicateType } from "../../sidebar/utils/types";
import { createId } from "../../../utils/id";

interface ActionPredicateModalProps {
  isOpen: boolean;
  node: CanvasNode | null;
  predicateTypes: PredicateType[];
  activeGroup: PredicateGroup;
  onChangeGroup: (group: PredicateGroup) => void;
  onClose: () => void;
  onAddPredicate: (
    nodeId: string,
    group: PredicateGroup,
    predicate: PredicateInstance
  ) => void;
  onRemovePredicate: (
    nodeId: string,
    group: PredicateGroup,
    predicateId: string
  ) => void;
}

const GROUP_LABELS: Record<PredicateGroup, string> = {
  precondition: "Preconditions",
  effect: "Effects",
};

const formatPredicateArgs = (
  predicate: PredicateInstance,
  definition?: PredicateType
): string => {
  if (!definition) {
    const entries = Object.entries(predicate.propertyValues ?? {});
    return entries.length
      ? entries.map(([key, value]) => `${key}=${value || "?"}`).join(", ")
      : "No parameters";
  }

  if (!definition.properties.length) {
    return "No parameters";
  }

  return definition.properties
    .map((property) => {
      const raw = predicate.propertyValues?.[property.id];
      const value = raw?.trim() || "?";
      return `${property.name || property.id}=${value}`;
    })
    .join(", ");
};

export default function ActionPredicateModal({
  isOpen,
  node,
  predicateTypes,
  activeGroup,
  onChangeGroup,
  onClose,
  onAddPredicate,
  onRemovePredicate,
}: ActionPredicateModalProps) {
  const predicateTypeMap = useMemo(
    () => new Map(predicateTypes.map((type) => [type.id, type] as const)),
    [predicateTypes]
  );

  const initialTypeId = predicateTypes[0]?.id ?? "";
  const initialDefinition = initialTypeId
    ? predicateTypes.find((type) => type.id === initialTypeId)
    : undefined;

  const buildEmptyValues = (definition?: PredicateType) => {
    if (!definition) {
      return {};
    }
    return definition.properties.reduce<Record<string, string>>(
      (acc, property) => {
        acc[property.id] = "";
        return acc;
      },
      {}
    );
  };

  const [selectedTypeId, setSelectedTypeId] = useState<string>(initialTypeId);
  const [formValues, setFormValues] = useState<Record<string, string>>(() =>
    buildEmptyValues(initialDefinition)
  );
  const [isNegated, setIsNegated] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [showForm, setShowForm] = useState(false);

  if (!isOpen || !node) {
    return null;
  }

  const filteredPredicateTypes = predicateTypes.filter((type) => {
    if (!searchQuery.trim()) {
      return true;
    }
    const query = searchQuery.trim().toLowerCase();
    return (
      type.name.toLowerCase().includes(query) ||
      type.id.toLowerCase().includes(query)
    );
  });

  const selectedType = selectedTypeId
    ? predicateTypeMap.get(selectedTypeId)
    : undefined;

  const activePredicates =
    activeGroup === "precondition" ? node.preconditions ?? [] : node.effects ?? [];

  const handleAddPredicate = () => {
    if (!selectedType) {
      return;
    }

    const propertyValues = selectedType.properties.reduce<Record<string, string>>(
      (acc, property) => {
        acc[property.id] = formValues[property.id]?.trim() ?? "";
        return acc;
      },
      {}
    );

    onAddPredicate(node.id, activeGroup, {
      id: createId("predicate"),
      typeId: selectedType.id,
      typeName: selectedType.name,
      propertyValues,
      isNegated,
    });

    setFormValues(buildEmptyValues(selectedType));
    setIsNegated(false);
    setShowForm(false);
  };

  const handleTypeChange = (nextTypeId: string) => {
    setSelectedTypeId(nextTypeId);
    const definition = predicateTypeMap.get(nextTypeId);
    setFormValues(buildEmptyValues(definition));
  };

  const handleSearchChange = (value: string) => {
    setSearchQuery(value);
    if (!value.trim()) {
      return;
    }
    const query = value.trim().toLowerCase();
    const nextList = predicateTypes.filter(
      (type) =>
        type.name.toLowerCase().includes(query) ||
        type.id.toLowerCase().includes(query)
    );
    if (!nextList.length) {
      setSelectedTypeId("");
      return;
    }
    if (!nextList.some((type) => type.id === selectedTypeId)) {
      handleTypeChange(nextList[0].id);
    }
  };

  const handleOpenForm = () => {
    setShowForm(true);
    const nextTypeId = filteredPredicateTypes[0]?.id ?? initialTypeId;
    if (nextTypeId && nextTypeId !== selectedTypeId) {
      handleTypeChange(nextTypeId);
    }
  };

  const handleCloseForm = () => {
    setShowForm(false);
  };

  const handleGroupChange = (group: PredicateGroup) => {
    onChangeGroup(group);
    setShowForm(false);
  };

  const handlePropertyChange = (propertyId: string, value: string) => {
    setFormValues((prev) => ({
      ...prev,
      [propertyId]: value,
    }));
  };

  return (
    <div
      className="canvas-predicate-modal-backdrop"
      role="dialog"
      aria-modal="true"
      onClick={onClose}
    >
      <div
        className="canvas-predicate-modal"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="canvas-predicate-modal-header">
          <div className="canvas-predicate-modal-title">
            <span className="canvas-predicate-modal-node-name">{node.name}</span>
            <span className="canvas-predicate-modal-node-type">{node.typeLabel}</span>
          </div>
          <button
            type="button"
            className="canvas-predicate-modal-close"
            onClick={onClose}
            aria-label="Close predicate editor"
          >
            ×
          </button>
        </div>

        <div className="canvas-predicate-modal-controls" role="tablist">
          {(Object.keys(GROUP_LABELS) as PredicateGroup[]).map((group) => (
            <button
              key={group}
              type="button"
              className={`canvas-node-action-btn${
                activeGroup === group ? " is-active" : ""
              }`}
              onClick={() => handleGroupChange(group)}
              role="tab"
              aria-selected={activeGroup === group}
            >
              {GROUP_LABELS[group]}
            </button>
          ))}
        </div>

        <div className="canvas-predicate-modal-body">
          <div className="canvas-node-state">
            {!showForm ? (
              <div className="canvas-node-state-group">
                <div className="canvas-node-state-header">
                  <span className="canvas-node-state-title">
                    {GROUP_LABELS[activeGroup]}
                  </span>
                </div>
                <div className="canvas-node-state-list">
                  {activePredicates.length === 0 ? (
                    <p className="canvas-node-state-empty">
                      No {GROUP_LABELS[activeGroup].toLowerCase()} yet.
                    </p>
                  ) : (
                    <div className="canvas-predicate-list">
                      {activePredicates.map((predicate) => {
                        const definition = predicateTypeMap.get(predicate.typeId);
                        const name =
                          definition?.name || predicate.typeName || "Predicate";
                        const argsText = formatPredicateArgs(predicate, definition);

                        return (
                          <div key={predicate.id} className="canvas-node-state-item">
                            <div className="canvas-node-state-body">
                              <span className="canvas-node-state-name">{name}</span>
                              <span className="canvas-node-state-args">{argsText}</span>
                              {predicate.isNegated ? (
                                <span className="canvas-node-state-meta">Negated</span>
                              ) : null}
                            </div>
                            <div className="canvas-node-state-actions">
                              <button
                                type="button"
                                className="canvas-node-state-btn"
                                onClick={() =>
                                  onRemovePredicate(node.id, activeGroup, predicate.id)
                                }
                                aria-label="Remove predicate"
                                title="Remove predicate"
                              >
                                ×
                              </button>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              </div>
            ) : null}

            <div className="canvas-node-state-group">
              {!showForm ? (
                <button
                  type="button"
                  className="canvas-node-action-btn is-compact"
                  onClick={handleOpenForm}
                >
                  Add {activeGroup === "precondition" ? "Precondition" : "Effect"}
                </button>
              ) : (
                <div className="canvas-predicate-form">
                  <label className="form-group">
                    <span className="modal-label">Search predicates</span>
                    <input
                      type="search"
                      className="modal-input"
                      value={searchQuery}
                      onChange={(event) => handleSearchChange(event.target.value)}
                      placeholder="Search by name"
                    />
                  </label>

                  <label className="form-group">
                    <span className="modal-label">Predicate type</span>
                    <select
                      className="modal-select"
                      value={selectedTypeId}
                      onChange={(event) => handleTypeChange(event.target.value)}
                      disabled={filteredPredicateTypes.length === 0}
                    >
                      {filteredPredicateTypes.length === 0 ? (
                        <option value="">No predicate types available</option>
                      ) : (
                        filteredPredicateTypes.map((type) => (
                          <option key={type.id} value={type.id}>
                            {type.name}
                          </option>
                        ))
                      )}
                    </select>
                  </label>

                  <label className="canvas-predicate-negate">
                    <input
                      type="checkbox"
                      checked={isNegated}
                      onChange={(event) => setIsNegated(event.target.checked)}
                    />
                    Negated
                  </label>

                  {selectedType?.properties.map((property) => (
                    <label key={property.id} className="form-group">
                      <span className="modal-label">
                        {property.name || property.id}
                      </span>
                      <input
                        type="text"
                        className="modal-input"
                        value={formValues[property.id] ?? ""}
                        onChange={(event) =>
                          handlePropertyChange(property.id, event.target.value)
                        }
                        placeholder={property.valueType}
                      />
                    </label>
                  ))}

                  <div className="canvas-predicate-form-actions">
                    <button
                      type="button"
                      className="canvas-node-action-btn"
                      onClick={handleCloseForm}
                    >
                      Back
                    </button>
                    <button
                      type="button"
                      className="canvas-node-action-btn"
                      onClick={handleAddPredicate}
                      disabled={!selectedType}
                    >
                      Add
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
