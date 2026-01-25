import { useState, useEffect, useRef, type ChangeEvent } from "react";
import "./Header.css";
import type {
  DropdownProps,
  HeaderProps,
  NormalizedDropdownItem,
} from "./types.ts";
import { UserMenu } from "./UserMenu";

/**
 * Component for a dropdown menu in the header.
 * @param param0 
 * @returns The Dropdown component. 
 */
function Dropdown({ title, items }: DropdownProps) {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const normalizedItems: NormalizedDropdownItem[] = items.map((entry) => {
    if (typeof entry === "string") {
      return { kind: "action", label: entry };
    }

    if (entry.kind === "file" || entry.kind === "divider" || entry.kind === "label") {
      return entry;
    }

    return { ...entry, kind: "action" };
  });

  /**
   * Closes the dropdown when clicking outside of it
   * @param event MouseEvent
   */
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  return (
    <div 
      className="dropdown"
      ref={dropdownRef}
    >
      <button 
        className="dropdown-trigger"
        onClick={() => setIsOpen(!isOpen)} 
      >
        {title}
      </button>
      
      {isOpen && (
        <div className="dropdown-menu">
          {normalizedItems.map((item, index) => {
            if (item.kind === "divider") {
              return (
                <div
                  key={`divider-${index}`}
                  className="dropdown-divider"
                  role="separator"
                />
              );
            }

            if (item.kind === "label") {
              return (
                <div
                  key={`label-${index}`}
                  className="dropdown-group-label"
                >
                  {item.label}
                </div>
              );
            }

            if (item.kind === "file") {
              const handleFileChange = (
                event: ChangeEvent<HTMLInputElement>
              ) => {
                const file = event.target.files?.[0];
                if (file) {
                  item.onFileSelect(file);
                }
                event.target.value = "";
                setIsOpen(false);
              };

              return (
                <label
                  key={`file-${index}`}
                  className="dropdown-item file-upload"
                >
                  <span className="dropdown-item-title">{item.label}</span>
                  {item.hint && (
                    <span className="dropdown-item-hint">{item.hint}</span>
                  )}
                  <input
                    type="file"
                    className="file-upload-input"
                    accept={item.accept ?? ".txt"}
                    onChange={handleFileChange}
                  />
                </label>
              );
            }

            return (
              <button
                key={`action-${index}`}
                className={`dropdown-item${
                  item.label === "Export Graph (JSON)" ? " dropdown-item-strong" : ""
                }`}
                type="button"
                onClick={() => {
                  item.onSelect?.();
                  setIsOpen(false);
                }}
                disabled={item.disabled}
              >
                {item.label}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

export default function Header({
  theme,
  onToggleTheme,
  onImportActionInstances,
  onExportCanvasGraph,
  onImportCanvasGraph,
  onOpenValidate,
}: HeaderProps) {
  const isDarkMode = theme === "dark";
  const fileMenuItems: DropdownProps["items"] = [
    { label: "New" },
    { label: "Open..." },
    { label: "Save" },
    { label: "Save As..." },
    { kind: "divider" },
    { kind: "label", label: "Graph" },
    {
      kind: "action",
      label: "Export Graph (JSON)",
      onSelect: onExportCanvasGraph,
    },
    {
      kind: "file",
      label: "Import Graph (JSON)",
      accept: ".json,application/json",
      onFileSelect: (file) => onImportCanvasGraph(file),
    },
    { kind: "divider" },
    { kind: "label", label: "Instances" },
    {
      kind: "file",
      label: "Import Action Instances",
      hint: "TXT upload (.txt)",
      accept: ".txt",
      onFileSelect: onImportActionInstances,
    },
  ];

  return (
    <header className="header">
      <div className="header-left">
        <nav className="header-nav">
          <Dropdown title="File" items={fileMenuItems} />
          <Dropdown
            title="Edit"
            items={["Undo", "Redo", "Cut", "Copy", "Paste", "Delete"]}
          />
          <Dropdown
            title="View"
            items={["Zoom In", "Zoom Out", "Reset Zoom", "Toggle Grid"]}
          />
          <Dropdown
            title="Tools"
            items={[
              {
                kind: "action",
                label: "Validate (MontiCore)",
                onSelect: onOpenValidate,
              },
            ]}
          />
        </nav>

        <div className="header-separator"></div>

        <div className="header-actions">
          <button className="icon-btn" title="Undo" aria-label="Undo">
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M9 14L4 9l5-5" />
              <path d="M4 9h10.5a5.5 5.5 0 0 1 5.5 5.5v0a5.5 5.5 0 0 1-5.5 5.5H11" />
            </svg>
          </button>

          <button className="icon-btn" title="Redo" aria-label="Redo">
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M15 14l5-5-5-5" />
              <path d="M20 9H9.5A5.5 5.5 0 0 0 4 14.5v0A5.5 5.5 0 0 0 9.5 20H13" />
            </svg>
          </button>
        </div>
      </div>

      <div className="header-right">
        <UserMenu />
        
        <button
          className="icon-btn theme-toggle"
          onClick={onToggleTheme}
          aria-label={`Switch to ${isDarkMode ? "light" : "dark"} mode`}
          title={isDarkMode ? "Switch to light mode" : "Switch to dark mode"}
        ></button>
        <button
          className="icon-btn theme-toggle"
          onClick={onToggleTheme}
          aria-label={`Switch to ${isDarkMode ? "light" : "dark"} mode`}
          title={isDarkMode ? "Switch to light mode" : "Switch to dark mode"}
        >
          {isDarkMode ? (
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
            </svg>
          ) : (
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <circle cx="12" cy="12" r="5" />
              <line x1="12" y1="1" x2="12" y2="3" />
              <line x1="12" y1="21" x2="12" y2="23" />
              <line x1="4.22" y1="4.22" x2="5.64" y2="5.64" />
              <line x1="18.36" y1="18.36" x2="19.78" y2="19.78" />
              <line x1="1" y1="12" x2="3" y2="12" />
              <line x1="21" y1="12" x2="23" y2="12" />
              <line x1="4.22" y1="19.78" x2="5.64" y2="18.36" />
              <line x1="18.36" y1="5.64" x2="19.78" y2="4.22" />
            </svg>
          )}
        </button>
      </div>
    </header>
  );
}