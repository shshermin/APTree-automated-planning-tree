import { useCallback, useEffect, useMemo, useState } from "react";
import { BASIC_TYPE_OPTIONS } from "../../../constants/basicTypes";
import { createId } from "../../../utils/id";
import type {
  ParameterType,
  TypeDefinitionModalProps,
  TypeProperty,
} from "../utils/types";

/**
 * creates an empty type property object.
 * @returns freshly created property placeholder
 */
function createEmptyProperty(): TypeProperty {
  return {
    id: createId("property"),
    name: "",
    valueType: "",
  };
}

/**
 * creates a deep clone of the provided property list to keep state immutable.
 * @param properties property list that should be cloned
 * @returns cloned property list
 */
function cloneProperties(properties: TypeProperty[]): TypeProperty[] {
  return properties.map((property) => ({ ...property }));
}

/**
 * normalizes the incoming property list for state consumption.
 * @param properties property list originating from the initial value
 * @returns cloned property list or an empty array when no properties exist
 */
function buildPropertyState(properties: TypeProperty[]): TypeProperty[] {
  return properties.length > 0 ? cloneProperties(properties) : [];
}

/**
 * checks whether the provided property collections differ.
 * @param nextProperties reference list used for comparison
 * @param currentProperties property list currently stored in state
 * @returns true when the property sets differ
 */
function havePropertiesChanged(
  nextProperties: TypeProperty[],
  currentProperties: TypeProperty[]
): boolean {
  if (nextProperties.length !== currentProperties.length) {
    return true;
  }

  return nextProperties.some((property, index) => {
    const current = currentProperties[index];
    if (!current) {
      return true;
    }

    return (
      current.id !== property.id ||
      current.name !== property.name ||
      current.valueType !== property.valueType
    );
  });
}

/**
 * sanitizes the properties by trimming whitespace.
 * @param properties array of properties to sanitize
 * @returns cloned property array with trimmed strings
 */
function sanitizeProperties(properties: TypeProperty[]): TypeProperty[] {
  return properties.map((property) => ({
    ...property,
    name: property.name.trim(),
    valueType: property.valueType.trim(),
  }));
}

/**
 * renders the parameter-type definition modal, including property editing controls.
 * @param props modal configuration including the draft type and callbacks
 * @returns modal markup or null when the modal is closed
 */
export default function TypeDefinitionModal({
  isOpen,
  mode,
  title,
  initialValue,
  onClose,
  onSave,
  nameLabel = "Type Name",
  namePlaceholder = "e.g., Sensor",
  baseTypeLabel = "Base Type",
  baseTypePlaceholder = "Select a base type...",
  showBaseTypeField = true,
  propertyLabel = "Properties",
  propertyNamePlaceholder = "property name",
  propertyTypePlaceholder = "Select type...",
  propertyHelperText,
  baseTypeOptions = BASIC_TYPE_OPTIONS,
  fixedBaseTypeValue,
}: TypeDefinitionModalProps) {
  const [nameValue, setNameValue] = useState(initialValue.name);
  const [baseType, setBaseType] = useState(
    fixedBaseTypeValue ?? initialValue.type
  );
  const [properties, setProperties] = useState<TypeProperty[]>(() =>
    buildPropertyState(initialValue.properties)
  );
  const [typeId, setTypeId] = useState(initialValue.id);

  const resolvedBaseType = useMemo(
    () => (fixedBaseTypeValue ?? baseType ?? "").trim(),
    [baseType, fixedBaseTypeValue]
  );

  useEffect(() => {
    if (nameValue !== initialValue.name) {
      setNameValue(initialValue.name);
    }

    const nextBaseType = fixedBaseTypeValue ?? initialValue.type;
    if (baseType !== nextBaseType) {
      setBaseType(nextBaseType);
    }

    const nextProperties = buildPropertyState(initialValue.properties);

    if (havePropertiesChanged(nextProperties, properties)) {
      setProperties(nextProperties);
    }

    if (typeId !== initialValue.id) {
      setTypeId(initialValue.id);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialValue, fixedBaseTypeValue]);

  const hasDuplicatePropertyNames = useMemo(() => {
    const seen = new Set<string>();
    return properties.some((property) => {
      const trimmedName = property.name.trim();
      if (!trimmedName) {
        return false;
      }
      if (seen.has(trimmedName)) {
        return true;
      }
      seen.add(trimmedName);
      return false;
    });
  }, [properties]);

  const isFormValid = useMemo(() => {
    const trimmedName = nameValue.trim();
    if (!trimmedName) {
      return false;
    }
    if (showBaseTypeField && !resolvedBaseType) {
      return false;
    }
    if (
      properties.some(
        (property) => !property.name.trim() || !property.valueType
      )
    ) {
      return false;
    }
    if (hasDuplicatePropertyNames) {
      return false;
    }
    return true;
  }, [
    nameValue,
    properties,
    hasDuplicatePropertyNames,
    showBaseTypeField,
    resolvedBaseType,
  ]);

  const handleAddProperty = () => {
    setProperties((prev) => [...prev, createEmptyProperty()]);
  };

  const handleRemoveProperty = (propertyId: string) => {
    setProperties((prev) =>
      prev.filter((property) => property.id !== propertyId)
    );
  };

  const updateProperty = useCallback(
    (propertyId: string, updates: Partial<TypeProperty>) => {
      setProperties((prev) =>
        prev.map((property) =>
          property.id === propertyId ? { ...property, ...updates } : property
        )
      );
    },
    []
  );

  const handlePropertyNameChange = (propertyId: string, value: string) => {
    updateProperty(propertyId, { name: value });
  };

  const handlePropertyTypeChange = (propertyId: string, value: string) => {
    updateProperty(propertyId, { valueType: value });
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!isFormValid) {
      return;
    }

    const sanitized = sanitizeProperties(properties);
    const next: ParameterType = {
      ...initialValue,
      id: typeId || createId("param-type"),
      name: nameValue.trim(),
      type: resolvedBaseType,
      properties: sanitized.map((property) => ({
        ...property,
        id: property.id || createId("property"),
      })),
    };

    onSave(next);
  };

  if (!isOpen) {
    return null;
  }

  return (
    <div className="modal-overlay type-modal-overlay">
      <div
        className="modal-content type-modal-content"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="modal-header">
          <h3>{title}</h3>
          <button className="modal-close-btn" onClick={onClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form type-modal-form">
          <div className="form-group">
            <label className="modal-label" htmlFor="type-name-input">
              {nameLabel}
            </label>
            <input
              id="type-name-input"
              className="modal-input"
              value={nameValue}
              onChange={(event) => setNameValue(event.target.value)}
              placeholder={namePlaceholder}
              type="text"
            />
          </div>

          {showBaseTypeField && (
            <div className="form-group">
              <label
                className="modal-label"
                htmlFor={fixedBaseTypeValue ? undefined : "type-base-select"}
              >
                {baseTypeLabel}
              </label>
              {fixedBaseTypeValue ? (
                <div
                  className="modal-static-value"
                  role="textbox"
                  aria-readonly="true"
                >
                  {fixedBaseTypeValue}
                </div>
              ) : (
                <select
                  id="type-base-select"
                  className="modal-select"
                  value={baseType}
                  onChange={(event) => setBaseType(event.target.value)}
                >
                  <option value="" disabled>
                    {baseTypePlaceholder}
                  </option>
                  {baseTypeOptions.map((option) => (
                    <option key={option} value={option}>
                      {option}
                    </option>
                  ))}
                </select>
              )}
            </div>
          )}

          <div className="form-group type-modal-properties">
            <div className="property-list-header">
              <span className="modal-label">{propertyLabel}</span>
              <button
                type="button"
                className="property-add-btn"
                onClick={handleAddProperty}
              >
                + Add Property
              </button>
            </div>

            {propertyHelperText && (
              <p className="property-helper-text">{propertyHelperText}</p>
            )}

            {properties.length === 0 ? (
              <p className="property-empty-hint">No properties defined yet.</p>
            ) : (
              <div className="property-list">
                {properties.map((property) => (
                  <div key={property.id} className="property-row">
                    <input
                      className="modal-input property-input"
                      type="text"
                      value={property.name}
                      onChange={(event) =>
                        handlePropertyNameChange(
                          property.id,
                          event.target.value
                        )
                      }
                      placeholder={propertyNamePlaceholder}
                    />
                    <select
                      className="modal-select property-select"
                      value={property.valueType}
                      onChange={(event) =>
                        handlePropertyTypeChange(
                          property.id,
                          event.target.value
                        )
                      }
                    >
                      <option value="" disabled>
                        {propertyTypePlaceholder}
                      </option>
                      {baseTypeOptions.map((option) => (
                        <option key={option} value={option}>
                          {option}
                        </option>
                      ))}
                    </select>
                    <button
                      type="button"
                      className="property-remove-btn"
                      onClick={() => handleRemoveProperty(property.id)}
                      aria-label="Remove property"
                      title="Remove property"
                    >
                      &times;
                    </button>
                  </div>
                ))}
              </div>
            )}

            {hasDuplicatePropertyNames && (
              <p className="property-error" role="alert">
                Property names must be unique.
              </p>
            )}
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-cancel" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn-save" disabled={!isFormValid}>
              {mode === "add" ? "Create Type" : "Save Changes"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
