import { useState, useCallback } from 'react';

// this is a simplified version of https://github.com/camfletch/react-editable-hooks/blob/master/src/index.ts

/** The return value for {@link useEditableState} */
export interface EditableState<T> {
  /**
   * Whether the field is currently being edited. This should be used to
   * determine whether to render an input for the field, or just a static view.
   */
  isEditing: boolean;
  /**
   * The 'temporary' editing value for the field. This should be used as the value
   * for whatever input is being used for the field.
   */
  editValue: T;
  /**
   * Sets the current editValue. This should be to handle change events on whatever
   * input is being used for the field.
   */
  setEditValue: (newValue: T) => void;
  /**
   * Callback to be used when initiating an edit
   */
  onEditBegin: () => void;
  /**
   * Callback to be used when an edit is 'confirmed'/'saved'
   */
  onEditConfirm: () => void;
  /**
   * Callback to be used when editing is cancelled (eg. via ESC, or clicking a
   * 'cancel' button)
   */
  onEditCancel: () => void;
}

/** The arguments for {@link useEditableState} */
export interface UseEditableStateArguments<T> {
  /**
   * The 'true' value for the editable field (eg. from the server).
   */
  value: T;
  /**
   * Callback that sets the 'true' value for the field (after an edit has been
   * confirmed).
   */
  onValueChanged: (newValue: T) => void;
}

/**
 * A custom hook for managing editable field state
 */
export function useEditableState<T>({ value, onValueChanged }: UseEditableStateArguments<T>): EditableState<T> {
  const [editValue, setEditValueRaw] = useState<T>(value);
  const [isEditing, setIsEditing] = useState(false);

  const onEditBegin = useCallback(() => {
    setEditValueRaw(value);
    setIsEditing(true);
  }, [value]);

  const onEditConfirm = useCallback(() => {
    onValueChanged(editValue);
    setIsEditing(false);
  }, [editValue, onValueChanged]);

  const onEditCancel = useCallback(() => {
    setEditValueRaw(value);
    setIsEditing(false);
  }, [value]);

  const setEditValue = useCallback((newValue: T) => {
    setEditValueRaw(newValue);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return {
    onEditBegin,
    onEditConfirm,
    onEditCancel,
    isEditing,
    editValue,
    setEditValue
  };
}
