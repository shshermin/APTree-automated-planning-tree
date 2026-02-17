import { useEffect, useRef, useState } from "react";
import type { EditModalProps } from "../utils/types";

/**
 * list of basic types for the dropdown.
 */
const BASIC_TYPES = [
  "Integer",
  "Double",
  "String",
  "Boolean",
  "Agent",
  "Element",
  "Location",
  "Layer",
  "Module",
  "Tool",
  "Object",
  "List",
  "Set",
  "Map",
];

/**
 * renders the generic edit modal used for simple planner entities and sections.
 * @param props modal configuration such as mode, callbacks, and labels
 * @returns modal markup or null when the modal is closed
 */
export default function EditModal({
  isOpen,
  title,
  initialValue,
  onClose,
  onSave,
  hideTypeField = false,
  nameLabel = "Name",
  namePlaceholder = "e.g., target_entity",
  helperText,
  saveLabel = "Save",
  enableDescriptionField = false,
  descriptionLabel = "Description",
  descriptionPlaceholder = "Describe this item...",
}: EditModalProps) {
  const [nameValue, setNameValue] = useState(initialValue.name);
  const [typeValue, setTypeValue] = useState(initialValue.type);
  const [descriptionValue, setDescriptionValue] = useState(initialValue.description ?? "");
  const [itemId, setItemId] = useState(initialValue.id);

  const nameInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    const frame = requestAnimationFrame(() => {
      setNameValue(initialValue.name);
      setTypeValue(initialValue.type);
      setDescriptionValue(initialValue.description ?? "");
      setItemId(initialValue.id);
    });

    return () => cancelAnimationFrame(frame);
  }, [
    isOpen,
    initialValue.id,
    initialValue.name,
    initialValue.type,
    initialValue.description,
  ]);

  /**
   * focuses the name input when the modal opens.
   * @returns void
   */
  useEffect(() => {
    if (isOpen && nameInputRef.current) {
      setTimeout(() => nameInputRef.current?.focus(), 50);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  /**
   * handles the form submission for items and sections.
   * @param event React.FormEvent submitted by the modal form
   * @returns void
   */
  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    const trimmedName = nameValue.trim();
    const trimmedType = typeValue.trim();

    if (hideTypeField) {
      if (trimmedName) {
        onSave({
          ...initialValue,
          id: itemId,
          name: trimmedName,
          type: initialValue.type ?? "",
          description: enableDescriptionField
            ? descriptionValue.trim()
            : initialValue.description,
        });
      }
    } else {
      if (trimmedName && trimmedType) {
        onSave({
          ...initialValue,
          id: itemId,
          name: trimmedName,
          type: trimmedType,
          description: enableDescriptionField
            ? descriptionValue.trim()
            : initialValue.description,
        });
      }
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h3>{title}</h3>
          <button className="modal-close-btn" onClick={onClose}>
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label className="modal-label" htmlFor="modal-name-input">
              {nameLabel}
            </label>
            <input
              ref={nameInputRef}
              type="text"
              className="modal-input"
              value={nameValue}
              onChange={(e) => setNameValue(e.target.value)}
              placeholder={namePlaceholder}
              id="modal-name-input"
            />
            {helperText && hideTypeField && (
              <p className="category-modal-text">{helperText}</p>
            )}
          </div>

          {!hideTypeField && (
            <div className="form-group">
              <label className="modal-label" htmlFor="modal-type-select">
                Type
              </label>
              <select
                className="modal-select"
                value={typeValue}
                onChange={(e) => setTypeValue(e.target.value)}
                id="modal-type-select"
              >
                <option value="" disabled>
                  Select a type...
                </option>
                {BASIC_TYPES.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>
          )}

          {enableDescriptionField ? (
            <div className="form-group">
              <label className="modal-label" htmlFor="modal-description-input">
                {descriptionLabel}
              </label>
              <textarea
                id="modal-description-input"
                className="modal-textarea"
                value={descriptionValue}
                onChange={(event) => setDescriptionValue(event.target.value)}
                placeholder={descriptionPlaceholder}
                rows={3}
              />
            </div>
          ) : null}

          <div className="modal-footer">
            <button type="button" className="btn-cancel" onClick={onClose}>
              Cancel
            </button>
            <button
              type="submit"
              className="btn-save"
              disabled={
                hideTypeField
                  ? !nameValue.trim()
                  : !nameValue.trim() || !typeValue.trim()
              }
            >
              {saveLabel}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
