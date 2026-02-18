import { useCallback, useRef, useState } from "react";

/** describes the shared shape for modal controller state. */
export interface ModalControllerState<TValue> {
  isOpen: boolean;
  mode: "add" | "edit";
  index: number | null;
  initialValue: TValue;
  revision: number;
}

/** exposes modal state along with the typical lifecycle helpers. */
export interface ModalController<TValue> {
  state: ModalControllerState<TValue>;
  openAdd: (initialValue?: TValue) => void;
  openEdit: (index: number, initialValue: TValue) => void;
  close: () => void;
}

/**
 * provides a monotonically increasing revision number for modal resets.
 * @returns function that returns the next revision identifier per invocation
 */
export const useRevisionGenerator = () => {
  const counterRef = useRef(0);

  return useCallback(() => {
    counterRef.current += 1;
    return counterRef.current;
  }, []);
};

/**
 * centralizes reusable modal state management helpers for add/edit flows.
 * @param createEmpty factory returning a blank value for the modal
 * @returns modal state alongside helpers for opening and closing the modal
 */
export const useModalController = <TValue,>(
  createEmpty: () => TValue
): ModalController<TValue> => {
  const nextRevision = useRevisionGenerator();
  const [state, setState] = useState<ModalControllerState<TValue>>(() => ({
    isOpen: false,
    mode: "add",
    index: null,
    initialValue: createEmpty(),
    revision: 0,
  }));

  /**
   * opens the modal in "add" mode with an optional initial value.
   * @param initialValue optional initial value to prefill the modal
   */
  const openAdd = (initialValue?: TValue) => {
    setState({
      isOpen: true,
      mode: "add",
      index: null,
      initialValue: initialValue ?? createEmpty(),
      revision: nextRevision(),
    });
  };

  /**
   * opens the modal in "edit" mode with the provided index and initial value.
   * @param index index of the item being edited
   * @param initialValue initial value to prefill the modal
   */
  const openEdit = (index: number, initialValue: TValue) => {
    setState({
      isOpen: true,
      mode: "edit",
      index,
      initialValue,
      revision: nextRevision(),
    });
  };

  /**
   * closes the modal and resets its state.
   */
  const close = () => {
    setState({
      isOpen: false,
      mode: "add",
      index: null,
      initialValue: createEmpty(),
      revision: nextRevision(),
    });
  };

  return { state, openAdd, openEdit, close };
};
