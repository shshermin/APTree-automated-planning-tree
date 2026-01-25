import { useEffect, useMemo, useState } from "react";
import { createId } from "../../../utils/id";
import type {
  ActionInstance,
  ActionInstanceModalProps,
  TypedInstance,
  TypedInstanceModalProps,
} from "../utils/types";

/** shared modal rendering logic used by all typed instance modals. */
function BaseInstanceModal<TInstance extends TypedInstance>(
  props: TypedInstanceModalProps<TInstance>
) {
  const {
    isOpen,
    mode,
    title,
    initialValue,
    typeDefinitions,
    onClose,
    onSave,
    nameLabel = "Instance Name",
    namePlaceholder = "e.g., selected_item",
    typeLabel = "Type",
    typePlaceholder = "Select a type...",
    propertyValuesLabel = "Property Values",
    propertyEmptyMessage,
    createButtonLabel = "Create Instance",
    saveButtonLabel = "Save Changes",
    baseTypePrefixLabel = "Base type",
    validatePropertyValue,
    propertyValidationHint,
  } = props;

  const [nameValue, setNameValue] = useState(initialValue.name);
  const [typeId, setTypeId] = useState(
    () => initialValue.typeId || typeDefinitions[0]?.id || ""
  );
  const [propertyValues, setPropertyValues] = useState<Record<string, string>>(
    () => ({ ...initialValue.propertyValues })
  );
  const instanceId = useMemo(
    () => initialValue.id || createId("instance"),
    [initialValue.id]
  );
  const [showValidation, setShowValidation] = useState(false);

  /**
   * synchronize internal state when initialValue or typeDefinitions change.
   * @returns void
   */
  useEffect(() => {
    if (nameValue !== initialValue.name) {
      setNameValue(initialValue.name);
    }

    const fallbackTypeId = initialValue.typeId || typeDefinitions[0]?.id || "";
    if (typeId !== fallbackTypeId) {
      setTypeId(fallbackTypeId);
    }

    const nextValues = { ...initialValue.propertyValues };
    const currentKeys = Object.keys(propertyValues);
    const nextKeys = Object.keys(nextValues);
    const valuesChanged =
      currentKeys.length !== nextKeys.length ||
      nextKeys.some((key) => propertyValues[key] !== nextValues[key]);

    if (valuesChanged) {
      setPropertyValues(nextValues);
    }

    setShowValidation(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialValue, typeDefinitions]);

  useEffect(() => {
    if (!isOpen && showValidation) {
      setShowValidation(false);
    }
  }, [isOpen, showValidation]);

  const selectedType = useMemo(
    () => typeDefinitions.find((type) => type.id === typeId) ?? null,
    [typeDefinitions, typeId]
  );

  const resolvedPropertyValues = useMemo(() => {
    if (!selectedType) {
      return {};
    }

    return selectedType.properties.reduce<Record<string, string>>(
      (acc, property) => {
        acc[property.id] = propertyValues[property.id] ?? "";
        return acc;
      },
      {}
    );
  }, [selectedType, propertyValues]);

  const propertyValidationState = useMemo(() => {
    if (!selectedType) {
      return {
        missing: new Set<string>(),
        invalid: new Set<string>(),
      };
    }

    const missing = new Set<string>();
    const invalid = new Set<string>();

    selectedType.properties.forEach((property) => {
      const rawValue = resolvedPropertyValues[property.id] ?? "";
      const trimmedValue = rawValue.trim();

      if (!trimmedValue) {
        missing.add(property.id);
        return;
      }

      if (
        validatePropertyValue &&
        !validatePropertyValue(trimmedValue, property)
      ) {
        invalid.add(property.id);
      }
    });

    return { missing, invalid };
  }, [selectedType, resolvedPropertyValues, validatePropertyValue]);

  const { missing: missingPropertyIds, invalid: invalidPropertyIds } =
    propertyValidationState;

  if (!isOpen) {
    return null;
  }

  const isFormDisabled = typeDefinitions.length === 0 || !selectedType;
  const trimmedName = nameValue.trim();
  const arePropertiesValid = selectedType
    ? missingPropertyIds.size === 0 && invalidPropertyIds.size === 0
    : false;
  const isFormValid =
    !isFormDisabled && Boolean(trimmedName) && arePropertiesValid;

  const emptyStateMessage = propertyEmptyMessage
    ? propertyEmptyMessage
    : selectedType
    ? "This type has no properties."
    : "Define a type to provide property values.";

    /**
     * handles changes to the selected type.
     * @param event change event from the type select field
     */
  const handleTypeChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const nextTypeId = event.target.value;
    setTypeId(nextTypeId);
    setPropertyValues({});
    setShowValidation(false);
  };

  /**
   * handles changes to a property value.
   * @param propertyId ID of the property being updated 
   * @param value new value for the property 
   */
  const handlePropertyValueChange = (propertyId: string, value: string) => {
    setPropertyValues((prev) => ({ ...prev, [propertyId]: value }));
  };

  /**
   * handles the form submission.
   * @param event form submission event 
   */
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!isFormValid || !selectedType) {
      setShowValidation(true);
      return;
    }
    setShowValidation(false);

    const sanitizedValues = selectedType.properties.reduce<
      Record<string, string>
    >((acc, property) => {
      acc[property.id] = (resolvedPropertyValues[property.id] ?? "").trim();
      return acc;
    }, {});

    onSave({
      ...(initialValue as TInstance),
      id: instanceId,
      name: trimmedName,
      type: selectedType.name,
      typeId: selectedType.id,
      propertyValues: sanitizedValues,
    });
  };

  return (
    <div className="modal-overlay">
      <div
        className="modal-content instance-modal"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="modal-header">
          <h3>{title}</h3>
          <button className="modal-close-btn" onClick={onClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label className="modal-label" htmlFor="instance-name-input">
              {nameLabel}
            </label>
            <input
              id="instance-name-input"
              className="modal-input"
              type="text"
              value={nameValue}
              onChange={(event) => setNameValue(event.target.value)}
              placeholder={namePlaceholder}
            />
          </div>

          <div className="form-group">
            <label className="modal-label" htmlFor="instance-type-select">
              {typeLabel}
            </label>
            <select
              id="instance-type-select"
              className="modal-select"
              value={typeId}
              onChange={handleTypeChange}
              disabled={typeDefinitions.length === 0}
            >
              <option value="" disabled>
                {typePlaceholder}
              </option>
              {typeDefinitions.map((definition) => (
                <option key={definition.id} value={definition.id}>
                  {definition.name}
                </option>
              ))}
            </select>
            {selectedType && (
              <p className="instance-type-hint">
                {baseTypePrefixLabel}: <strong>{selectedType.type || "n/a"}</strong>
              </p>
            )}
          </div>

          <div className="form-group">
            <span className="modal-label">{propertyValuesLabel}</span>
            {selectedType && selectedType.properties.length > 0 ? (
              <div className="instance-properties">
                {selectedType.properties.map((property) => {
                  const fieldId = `instance-${property.id}`;
                  const hasInvalidPattern = invalidPropertyIds.has(property.id);
                  const isMissingValue = missingPropertyIds.has(property.id);
                  const shouldHighlight =
                    showValidation && (hasInvalidPattern || isMissingValue);
                  return (
                    <div key={property.id} className="instance-property-row">
                      <label
                        className="instance-property-label"
                        htmlFor={fieldId}
                      >
                        {property.name}
                        <span className="instance-property-type">
                          {property.valueType}
                        </span>
                      </label>
                      <input
                        id={fieldId}
                        className={`modal-input instance-property-input${shouldHighlight ? " input-invalid" : ""}`}
                        type="text"
                        value={resolvedPropertyValues[property.id] ?? ""}
                        onChange={(event) =>
                          handlePropertyValueChange(
                            property.id,
                            event.target.value
                          )
                        }
                        placeholder={`Enter ${property.valueType}`}
                        aria-invalid={shouldHighlight || undefined}
                      />
                      {showValidation && hasInvalidPattern && propertyValidationHint ? (
                        <p className="input-error-message">
                          {propertyValidationHint}
                        </p>
                      ) : null}
                      {showValidation && !hasInvalidPattern && isMissingValue ? (
                        <p className="input-error-message">Value required.</p>
                      ) : null}
                    </div>
                  );
                })}
              </div>
            ) : (
              <p className="instance-property-empty">{emptyStateMessage}</p>
            )}
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-cancel" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn-save" disabled={!isFormValid}>
              {mode === "add" ? createButtonLabel : saveButtonLabel}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

/** action instance modal configured with action-centric copy. */
export function ActionInstanceModal(props: ActionInstanceModalProps) {
  return (
    <BaseInstanceModal<ActionInstance>
      {...props}
      nameLabel={props.nameLabel ?? "Action Instance Name"}
      namePlaceholder={props.namePlaceholder ?? "e.g., pick_up_wrench"}
      typeLabel={props.typeLabel ?? "Action Type"}
      typePlaceholder={props.typePlaceholder ?? "Select an action type..."}
      propertyEmptyMessage={
        props.propertyEmptyMessage ??
        "This action type does not define any properties."
      }
      propertyValuesLabel={props.propertyValuesLabel ?? "Action Property Values"}
      baseTypePrefixLabel={props.baseTypePrefixLabel ?? "Base action"}
      createButtonLabel={props.createButtonLabel ?? "Create Action Instance"}
      saveButtonLabel={props.saveButtonLabel ?? "Save Action Instance"}
    />
  );
}
