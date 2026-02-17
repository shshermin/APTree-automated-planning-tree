import { useState } from "react";
import EditModal from "./modals/EditModal";
import { ActionInstanceModal } from "./modals/InstanceModal";
import TypeDefinitionModal from "./modals/TypeDefinitionModal";
import BtNodeWizardModal, { type WizardStage } from "./modals/BtNodeWizardModal";
import "./Sidebar.css";
import { CategoryItemList } from "./components/CategoryItemList";
import SidebarSection from "./components/SidebarSection";
import {
  CANVAS_TOOL_DRAG_DATA_FORMAT,
  type DraggedCanvasTool,
} from "../editor/dragTypes";
import {
  ACTION_INSTANCES_KEY,
  ACTION_TYPES_KEY,
  BLACKBOARD_KEY,
  BT_NODES_KEY,
  DECORATOR_NODES_KEY,
  SERVICE_NODES_KEY,
} from "./utils/constants";
import type { BehaviorNodeOption, SidebarManager, StructuredItem } from "./utils/types";

interface SidebarProps {
  manager: SidebarManager;
  onCreateBehaviorNode?: (option: BehaviorNodeOption) => void;
}

/**
 * assembles the full planner sidebar, wiring state-driven modals and section content together.
 * @returns sidebar layout containing sections, search inputs, and supporting modals
 */
export default function Sidebar({ manager, onCreateBehaviorNode }: SidebarProps) {
  const {
    addLabelFor,
    categoryModal,
    categoryOrder,
    categoryTitles,
    closeCategoryModal,
    closeActionInstanceModal,
    closeModal,
    closeParameterTypeModal,
    closePredicateTypeModal,
    closeActionTypeModal,
    getItemsForCategory,
    handleDeleteCategory,
    handleDeleteItem,
    handleSaveCategory,
    handleSaveFromModal,
    handleSaveActionInstance,
    handleSaveParameterType,
    handleSavePredicateType,
    handleSaveActionType,
    handleSearchChange,
    actionInstanceModalState,
    modalState,
    openAddModal,
    openEditModal,
    openRenameCategoryModal,
    actionTypeMap,
    actionTypes,
    searchQueries,
    parameterTypeModalState,
    predicateTypeModalState,
    actionTypeModalState,
    decoratorNodeOptions,
    serviceNodeOptions,
    flowNodeOptions,
  } = manager;

  const [isBtNodeWizardOpen, setBtNodeWizardOpen] = useState(false);
  const [wizardHighlightStage, setWizardHighlightStage] = useState<WizardStage | null>(null);
  const flowOptions = flowNodeOptions;
  const decoratorOptions = decoratorNodeOptions;
  const serviceOptions = serviceNodeOptions;
  const visibleCategories = categoryOrder.filter(
    (key) => key !== ACTION_INSTANCES_KEY
  );
  const isBehaviorNodeModal =
    modalState.category === DECORATOR_NODES_KEY ||
    modalState.category === SERVICE_NODES_KEY;
  const behaviorNodeNameLabel =
    modalState.category === DECORATOR_NODES_KEY
      ? "Decorator Name"
      : modalState.category === SERVICE_NODES_KEY
      ? "Service Name"
      : "Display Name";
  const behaviorNodePlaceholder =
    modalState.category === DECORATOR_NODES_KEY
      ? "e.g., cooldown"
      : modalState.category === SERVICE_NODES_KEY
      ? "e.g., sensing_service"
      : "e.g., target_entity";
  const behaviorNodeModalTitle = isBehaviorNodeModal
    ? modalState.mode === "add"
      ? modalState.category === DECORATOR_NODES_KEY
        ? "Add Decorator Node"
        : "Add Service Node"
      : modalState.category === DECORATOR_NODES_KEY
      ? "Edit Decorator Node"
      : "Edit Service Node"
    : modalState.mode === "add"
    ? "Add Item"
    : "Edit Item";

    /**
     * hands builds a unique key for stateful modals to reset internal state on open.
     * @param mode modal mode  
     * @param index item index   
     * @param id item id 
     * @param revision item revision number 
     * @returns unique modal key 
     */
  const buildStatefulModalKey = (
    mode: "add" | "edit",
    index: number | null,
    id: string,
    revision: number
  ) => `${mode}-${index ?? "new"}-${id}-${revision}`;

  const categoryModalTitle =
    categoryModal.mode === "add" ? "Add Section" : "Rename Section";
  const categoryModalHelper =
    categoryModal.mode === "add"
      ? "Sections group related planner data. Use a short, descriptive title."
      : undefined;
  const categoryModalSaveLabel =
    categoryModal.mode === "add" ? "Create Section" : "Save";

  /**
   * opens the behavior tree node creation wizard.
   * @param highlightStage optional wizard stage to highlight 
   */
  const openBtNodeWizard = (highlightStage: WizardStage | null = null) => {
    setWizardHighlightStage(highlightStage === "root" ? null : highlightStage);
    setBtNodeWizardOpen(true);
  };

  /**
   * closes the behavior tree node creation wizard.
   */
  const closeBtNodeWizard = () => {
    setWizardHighlightStage(null);
    setBtNodeWizardOpen(false);
  };

  /**
   * handles selection of a behavior node option from the wizard.
   * @param option selected behavior node option 
   */
  const handleWizardSelectBehaviorOption = (option: BehaviorNodeOption) => {
    onCreateBehaviorNode?.(option);
  };

  return (
    <div className="sidebar">
      <div className="sidebar-title">
        <span className="sidebar-title-text">AI Planner</span>
      </div>

      <SidebarSection title="Canvas Tools" iconLabel="T" isOpen={false}>
        <div className="canvas-tools">
          <div
            className="canvas-tool-item"
            draggable
            role="button"
            tabIndex={0}
            aria-label="Separator Line"
            onDragStart={(event) => {
              const payload: DraggedCanvasTool = { tool: "separatorLine" };
              event.dataTransfer.setData(
                CANVAS_TOOL_DRAG_DATA_FORMAT,
                JSON.stringify(payload)
              );
              event.dataTransfer.effectAllowed = "copy";
            }}
            onKeyDown={(event) => {
              // Drag & drop is mouse-driven; keep keyboard focusable without action.
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
              }
            }}
          >
            <div className="canvas-tool-separator-preview">
              <span className="canvas-tool-separator-line" aria-hidden />
              <span className="canvas-tool-separator-label">Separator Line</span>
            </div>
          </div>
        </div>
      </SidebarSection>

      <BtNodeWizardModal
        isOpen={isBtNodeWizardOpen}
        flowOptions={flowOptions}
        decoratorOptions={decoratorOptions}
        serviceOptions={serviceOptions}
        onClose={closeBtNodeWizard}
        onSelectActionType={() => openAddModal(ACTION_TYPES_KEY)}
        onSelectActionInstance={() => openAddModal(ACTION_INSTANCES_KEY)}
        onSelectBehaviorOption={handleWizardSelectBehaviorOption}
        emphasizedStage={
          wizardHighlightStage && wizardHighlightStage !== "root"
            ? wizardHighlightStage
            : null
        }
      />

      <TypeDefinitionModal
        key={buildStatefulModalKey(
          parameterTypeModalState.mode,
          parameterTypeModalState.index,
          parameterTypeModalState.initialValue.id,
          parameterTypeModalState.revision
        )}
        isOpen={parameterTypeModalState.isOpen}
        mode={parameterTypeModalState.mode}
        title={
          parameterTypeModalState.mode === "add"
            ? "Add Parameter Type"
            : "Edit Parameter Type"
        }
        initialValue={parameterTypeModalState.initialValue}
        onClose={closeParameterTypeModal}
        onSave={handleSaveParameterType}
        baseTypeLabel="Basic Type"
        baseTypePlaceholder="Select basic type..."
        propertyLabel="Parameter Properties"
        propertyNamePlaceholder="parameter name"
        propertyTypePlaceholder="Select basic type..."
      />

      <TypeDefinitionModal
        key={buildStatefulModalKey(
          predicateTypeModalState.mode,
          predicateTypeModalState.index,
          predicateTypeModalState.initialValue.id,
          predicateTypeModalState.revision
        )}
        isOpen={predicateTypeModalState.isOpen}
        mode={predicateTypeModalState.mode}
        title={
          predicateTypeModalState.mode === "add"
            ? "Add Predicate Type"
            : "Edit Predicate Type"
        }
        initialValue={predicateTypeModalState.initialValue}
        onClose={closePredicateTypeModal}
        onSave={handleSavePredicateType}
        nameLabel="Predicate Type Name"
        namePlaceholder="e.g., is_reachable"
        baseTypeLabel="Predicate Base Type"
        baseTypePlaceholder="Select a base type..."
        propertyLabel="Predicate Parameters"
        propertyNamePlaceholder="predicate parameter name"
        propertyTypePlaceholder="Select basic type..."
        fixedBaseTypeValue="predicate"
      />

      <TypeDefinitionModal
        key={buildStatefulModalKey(
          actionTypeModalState.mode,
          actionTypeModalState.index,
          actionTypeModalState.initialValue.id,
          actionTypeModalState.revision
        )}
        isOpen={actionTypeModalState.isOpen}
        mode={actionTypeModalState.mode}
        title={
          actionTypeModalState.mode === "add"
            ? "Add Action Type"
            : "Edit Action Type"
        }
        initialValue={actionTypeModalState.initialValue}
        onClose={closeActionTypeModal}
        onSave={handleSaveActionType}
        nameLabel="Action Type Name"
        namePlaceholder="e.g., pick_up"
        baseTypeLabel="Action Base Type"
        baseTypePlaceholder="Select a base type..."
        propertyLabel="Action Parameters"
        propertyNamePlaceholder="action parameter name"
        propertyTypePlaceholder="Select basic type..."
        fixedBaseTypeValue="GenericBTAction"
      />

      <ActionInstanceModal
        key={buildStatefulModalKey(
          actionInstanceModalState.mode,
          actionInstanceModalState.index,
          actionInstanceModalState.initialValue.id,
          actionInstanceModalState.revision
        )}
        isOpen={actionInstanceModalState.isOpen}
        mode={actionInstanceModalState.mode}
        title={
          actionInstanceModalState.mode === "add"
            ? "Add Action Instance"
            : "Edit Action Instance"
        }
        initialValue={actionInstanceModalState.initialValue}
        typeDefinitions={actionTypes}
        onClose={closeActionInstanceModal}
        onSave={handleSaveActionInstance}
      />

      <EditModal
        key={`${modalState.mode}-${modalState.index}`}
        isOpen={modalState.isOpen}
        title={behaviorNodeModalTitle}
        initialValue={modalState.initialValue}
        onClose={closeModal}
        onSave={handleSaveFromModal}
        hideTypeField={isBehaviorNodeModal}
        nameLabel={behaviorNodeNameLabel}
        namePlaceholder={behaviorNodePlaceholder}
        enableDescriptionField={isBehaviorNodeModal}
        descriptionPlaceholder="Summarize how this node behaves..."
      />

      <EditModal
        key={`${categoryModal.mode}-${categoryModal.activeKey ?? "new"}`}
        isOpen={categoryModal.isOpen}
        title={categoryModalTitle}
        initialValue={categoryModal.value}
        onClose={closeCategoryModal}
        onSave={handleSaveCategory}
        hideTypeField
        nameLabel="Section Title"
        namePlaceholder="e.g., Sensors"
        helperText={categoryModalHelper}
        saveLabel={categoryModalSaveLabel}
      />

      {visibleCategories.map((categoryKey) => {
        const displayTitle = categoryTitles[categoryKey] ?? categoryKey;
        const iconLabel = displayTitle.charAt(0).toUpperCase();
        const buttonLabel = addLabelFor(categoryKey);
        const searchQuery = searchQueries[categoryKey] ?? "";
        const items = getItemsForCategory(categoryKey);
        const isBehaviorNodeCategory = categoryKey === BT_NODES_KEY;
        const isBlackboardCategory = categoryKey === BLACKBOARD_KEY;
        const canManageCategory = !isBlackboardCategory;
        const actionInstanceItems: StructuredItem[] = isBehaviorNodeCategory
          ? getItemsForCategory(ACTION_INSTANCES_KEY)
          : [];

        return (
          <SidebarSection
            key={categoryKey}
            title={displayTitle}
            isOpen={false}
            iconLabel={iconLabel}
            onEdit=
              {canManageCategory
                ? () => openRenameCategoryModal(categoryKey)
                : undefined}
            onDelete=
              {canManageCategory
                ? () => handleDeleteCategory(categoryKey)
                : undefined}
          >
            {canManageCategory &&
              (isBehaviorNodeCategory ? (
              <button
                className="add-button"
                onClick={() => openBtNodeWizard(null)}
                type="button"
              >
                + {buttonLabel}
              </button>
              ) : (
              <button
                className="add-button"
                onClick={() => openAddModal(categoryKey)}
                type="button"
              >
                + {buttonLabel}
              </button>
              ))}
            <div className="section-search">
              <input
                type="search"
                className="section-search-input"
                value={searchQuery}
                onChange={(event) =>
                  handleSearchChange(categoryKey, event.target.value)
                }
                placeholder="Search..."
                aria-label={`Search ${displayTitle}`}
              />
            </div>
            <CategoryItemList
              category={categoryKey}
              items={items}
              actionTypes={actionTypes}
              actionTypeMap={actionTypeMap}
              searchQuery={searchQuery}
              onEdit={openEditModal}
              onDelete={handleDeleteItem}
              readOnly={isBlackboardCategory}
            />
            {isBehaviorNodeCategory && (
              <div className="section-subgroup">
                <p className="section-subheading">Action Instances</p>
                <CategoryItemList
                  category={ACTION_INSTANCES_KEY}
                  items={actionInstanceItems}
                  actionTypes={actionTypes}
                  actionTypeMap={actionTypeMap}
                  searchQuery={searchQuery}
                  onEdit={openEditModal}
                  onDelete={handleDeleteItem}
                />
              </div>
            )}
          </SidebarSection>
        );
      })}
    </div>
  );
}
