export interface HeaderProps {
  theme: "light" | "dark";
  onToggleTheme: () => void;
  onImportActionInstances: (file: File) => void;
  onExportCanvasGraph: () => void;
  onImportCanvasGraph: (file: File) => void;
  onOpenValidate: () => void;
}

export interface DropdownActionItem {
  kind?: "action";
  label: string;
  onSelect?: () => void;
  disabled?: boolean;
}

export interface DropdownFileItem {
  kind: "file";
  label: string;
  hint?: string;
  accept?: string;
  onFileSelect: (file: File) => void;
}

export interface DropdownDividerItem {
  kind: "divider";
}

export interface DropdownLabelItem {
  kind: "label";
  label: string;
}

export type DropdownMenuItem =
  | string
  | DropdownActionItem
  | DropdownFileItem
  | DropdownDividerItem
  | DropdownLabelItem;

export type NormalizedDropdownItem =
  | (DropdownActionItem & { kind: "action" })
  | DropdownFileItem
  | DropdownDividerItem
  | DropdownLabelItem;

export interface DropdownProps {
  title: string;
  items: DropdownMenuItem[];
}
