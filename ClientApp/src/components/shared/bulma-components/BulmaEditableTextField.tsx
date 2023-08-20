import React, { useEffect, useRef, SyntheticEvent, CSSProperties } from 'react';
import { useEditableState, UseEditableStateArguments, EditableState } from '../hooks/editable-hook';
import { useKeyboardEvent } from '../hooks/keyboard-hook';
import { useOnClickOutside } from 'usehooks-ts';

const EditableTextField = ({ value, onValueChanged }: UseEditableStateArguments<string>) => {
  const { onEditBegin, onEditConfirm, onEditCancel, isEditing, editValue, setEditValue }: EditableState<string> =
    useEditableState({
      value,
      onValueChanged
    });

  // creating the ref by passing initial value null
  // The type of our ref is an input element
  const editInputRef = useRef<HTMLInputElement>(null);

  // The function passed to useEffect will run after the render is committed to the screen.
  // 1. If you don’t pass an array into the useEffect Hook, your component will continuously reload repeatedly.
  // 2. If you pass an empty array, we are not watching any variables, and therefore it will only update state on the first render, exactly like componentDidMount.
  // 3. By default, useEffect looks to see if the array values are different and if they are different, the arrow function is automatically called.
  useEffect(() => {
    if (editInputRef.current) {
      editInputRef.current.focus();
    }
  }, []);

  // add handlers for escape and return keys
  const cancelEditHandler = () => {
    if (isEditing) {
      onEditCancel();
    }
  };

  const confirmEditHandler = () => {
    if (isEditing) {
      onEditConfirm();
    }
  };

  useKeyboardEvent('Escape', cancelEditHandler);
  useKeyboardEvent('Enter', confirmEditHandler);

  // add handlers for clicking outside the input element
  const editWrapperRef = useRef<HTMLDivElement>(null);

  // add handler for clicking outside the input
  useOnClickOutside(editWrapperRef, () => {
    if (isEditing) {
      onEditCancel();
    }
  });

  // add onClick handler to the a href
  const handleClick = (e: SyntheticEvent) => {
    e.preventDefault();
    onEditBegin();
  };

  const onEditClear = (e: SyntheticEvent) => {
    e.preventDefault();
    setEditValue('');
  };

  const inputStyle: CSSProperties = {
    // width: '80%'
  };

  if (isEditing) {
    return (
      <form>
        <div className="field is-grouped" ref={editWrapperRef}>
          <div className="control is-expanded has-icons-right">
            <input
              type="text"
              className="input is-small"
              ref={editInputRef}
              value={editValue}
              onChange={(event) => setEditValue(event.target.value)}
              style={inputStyle}
            />
            <button type="button" className="icon is-small is-right is-icon-button" onClick={onEditClear}>
              <i className="fas fa-times fa-xs" />
            </button>
          </div>
          <p className="control">
            <button type="button" className="button is-info is-small" onClick={onEditConfirm}>
              <i className="fas fa-check" />
            </button>
            <button type="button" className="button is-small" onClick={onEditCancel}>
              <i className="fas fa-times" />
            </button>
          </p>
        </div>
      </form>
    );
  }
  return (
    // <button type="button" className="button is-text is-editable" onClick={onEditBegin}>
    //   {value}
    // </button>
    // eslint-disable-next-line jsx-a11y/anchor-is-valid
    <a href="#" className="is-editable is-vcentered" onClick={handleClick}>
      {value || <i>Empty</i>}
    </a>
  );
};

export const BulmaEditableTextField = EditableTextField;
