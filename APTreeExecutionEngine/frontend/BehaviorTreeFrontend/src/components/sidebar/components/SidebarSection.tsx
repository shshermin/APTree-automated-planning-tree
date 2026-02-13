import { useState } from "react";
import type { SectionProps } from "../utils/types";

/**
 * renders a collapsible sidebar section with optional rename/delete actions.
 * @param props section configuration including title, default open state, and action handlers
 * @returns collapsible container that wraps arbitrary sidebar content
 */
export default function SidebarSection({
  title,
  children,
  isOpen = false,
  iconLabel,
  onEdit,
  onDelete,
  disableDelete = false,
}: SectionProps) {
  const [open, setOpen] = useState(isOpen);
  const displayIcon = iconLabel ?? title.charAt(0).toUpperCase();

  const handleToggle = () => setOpen((prev) => !prev);

  const deleteTitle = disableDelete
    ? "Default sections cannot be removed"
    : "Delete section";

  return (
    <div className="sidebar-section">
      <div className="sidebar-section-header">
        <button
          type="button"
          className="sidebar-section-toggle"
          onClick={handleToggle}
          aria-expanded={open}
          aria-label={`${open ? "Collapse" : "Expand"} ${title}`}
        >
          <div className="sidebar-section-header-left">
            <span className="section-icon" aria-hidden>
              {displayIcon}
            </span>
            <strong>{title}</strong>
          </div>
          <span className="toggle-icon" aria-hidden>
            {open ? "▲" : "▼"}
          </span>
        </button>

        {(onEdit || onDelete) && (
          <div className="sidebar-section-actions">
            {onEdit && (
              <button
                type="button"
                className="section-action-btn"
                onClick={onEdit}
                title="Rename section"
                aria-label={`Rename section ${title}`}
              >
                ✎
              </button>
            )}
            {onDelete && (
              <button
                type="button"
                className="section-action-btn delete-action"
                onClick={onDelete}
                title={deleteTitle}
                aria-label={deleteTitle}
                disabled={disableDelete}
              >
                ×
              </button>
            )}
          </div>
        )}
      </div>
      {open && <div className="sidebar-content">{children}</div>}
    </div>
  );
}
