import React, { Component, useEffect, useRef, SyntheticEvent, CSSProperties } from 'react';
import ReactTable, { Column, RowInfo, CellInfo } from 'react-table';
import selectTableHOC, {
  SelectAllInputComponentProps,
  SelectInputComponentProps,
  SelectTableAdditionalProps
} from 'react-table/lib/hoc/selectTable';

import 'react-table/react-table.css';
import './my-react-table.scss';
import produce, { Draft } from 'immer';
import { useEditableState, UseEditableStateArguments, EditableState } from '../shared/hooks/editable-hook';
import { useKeyboardEvent } from '../shared/hooks/keyboard-hook';
import { useOutsideClick } from '../shared/hooks/outside-click-hook';
import { getUniqueData } from './TableExampleData';

const SelectTable = selectTableHOC(ReactTable);

function SelectInput({ selectType, onClick, id, checked, row }: SelectInputComponentProps) {
  return (
    <input
      type={selectType || 'checkbox'}
      aria-label={`${checked ? 'Un-select' : 'Select'} row with id:${id}`}
      checked={checked}
      id={id}
      onClick={(e) => {
        const { shiftKey } = e;
        e.stopPropagation();
        onClick(id, shiftKey, row);
      }}
      onChange={undefined}
    />
  );
}

function SelectAllInput({ selectType, onClick, checked }: SelectAllInputComponentProps) {
  return (
    <input
      type={selectType || 'checkbox'}
      aria-label={`${checked ? 'Un-select all' : 'Select all'}`}
      checked={checked}
      onClick={(e) => {
        e.stopPropagation();
        onClick();
      }}
      onChange={undefined}
    />
  );
}

const selectTableAdditionalProps: SelectTableAdditionalProps = {
  keyField: 'id',
  selectType: 'checkbox',
  selectWidth: 50,
  SelectInputComponent: SelectInput,
  SelectAllInputComponent: SelectAllInput
};

function EditableTextField({ value, onValueChanged }: UseEditableStateArguments<string>) {
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
  });

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
  useOutsideClick(editWrapperRef, () => {
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
            <button type="button" className="icon is-right editable-clear is-small" onClick={onEditClear}>
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
    // <button type="button" className="btn btn-link editable-click" onClick={onEditBegin}>
    //   {value}
    // </button>
    // eslint-disable-next-line jsx-a11y/anchor-is-valid
    <a href="#" className="editable-click" onClick={handleClick}>
      {value || <i>Empty</i>}
    </a>
  );
}

interface TableExample1Props {}

interface TableExample1State {
  data: any[];
  selection: any[];
  selectAll: boolean;
}

export default class TableExample1 extends Component<TableExample1Props, TableExample1State> {
  private selectTable: any = null; // add any to avoid getWrappedInstance() might be null

  constructor(props: TableExample1Props) {
    super(props);
    this.state = {
      data: getUniqueData(),
      selection: [],
      selectAll: false
    };
  }

  handleValueChanged = (cellInfo: CellInfo, newValue: string) => {
    // this.setState(prevState => {
    //   // creating copy of state variable data
    //   const data = [...prevState.data];
    //   data[cellInfo.index][cellInfo.column.id!] = newValue;

    //   return { data };
    // });

    // use immer
    this.setState(
      produce((draft: Draft<TableExample1State>) => {
        draft.data[cellInfo.index][cellInfo.column.id!] = newValue;
      })
    );
  };

  renderEditable = (cellInfo: CellInfo) => {
    return <EditableTextField value={cellInfo.value} onValueChanged={this.handleValueChanged.bind(null, cellInfo)} />;
  };

  toggleSelection = (key: string) => {
    // this.setState(prevState => {
    //   // creating copy of state variable selection
    //   let selection = [...prevState.selection];

    //   const keyIndex = selection.indexOf(key);

    //   // check to see if the key exists
    //   if (keyIndex >= 0) {
    //     // it does exist so we will remove it using destructing
    //     selection = [...selection.slice(0, keyIndex), ...selection.slice(keyIndex + 1)];
    //   } else {
    //     // it does not exist so add it
    //     selection.push(key);
    //   }

    //   return { selection };
    // });

    // use immer
    this.setState(
      produce((draft: Draft<TableExample1State>) => {
        const keyIndex = draft.selection.indexOf(key);

        // check to see if the key exists
        if (keyIndex >= 0) {
          draft.selection.splice(keyIndex, 1);
        } else {
          // it does not exist so add it
          draft.selection.push(key);
        }
      })
    );
  };

  toggleAll = () => {
    const { keyField } = selectTableAdditionalProps;

    // this.setState(prevState => {
    //   // toggle
    //   const selectAll = !prevState.selectAll;

    //   const selection = [] as any[];
    //   if (selectAll) {
    //     // we need to get at the internals of ReactTable
    //     const wrappedInstance = this.selectTable.getWrappedInstance();
    //     // the 'sortedData' property contains the currently accessible records based on the filter and sort
    //     const currentRecords = wrappedInstance.getResolvedState().sortedData;
    //     // we just push all the IDs onto the selection array
    //     currentRecords.forEach((item: any) => {
    //       if (item._original) {
    //         selection.push(`select-${item._original[keyField!]}`);
    //       }
    //     });
    //   }

    //   return { selectAll, selection };
    // });

    // use immer
    this.setState(
      produce((draft: Draft<TableExample1State>) => {
        // toggle
        draft.selectAll = !draft.selectAll;
        const selection = [] as any[];
        if (draft.selectAll) {
          // we need to get at the internals of ReactTable
          const wrappedInstance = this.selectTable.getWrappedInstance();
          // the 'sortedData' property contains the currently accessible records based on the filter and sort
          const currentRecords = wrappedInstance.getResolvedState().sortedData;
          // we just push all the IDs onto the selection array
          currentRecords.forEach((item: any) => {
            if (item._original) {
              selection.push(`select-${item._original[keyField!]}`);
            }
          });
        }

        draft.selection = selection;
      })
    );
  };

  isSelected = (key: string) => {
    return this.state.selection.includes(`select-${key}`);
  };

  // rowFn = (state: TableExample1State, rowInfo: RowInfo | undefined, column: Column | undefined, instance: any) => {
  rowFn = (state: TableExample1State, rowInfo: RowInfo | undefined) => {
    const { selection } = this.state;

    if (rowInfo && rowInfo !== undefined && rowInfo.row) {
      return {
        // onClick: (e: any, handleOriginal: any) => {
        //   console.log('It was in this row:', rowInfo);

        //   // toggle selection
        //   // this.toggleSelection(`select-${rowInfo.original.id}`, false, '');

        //   // IMPORTANT! React-Table uses onClick internally to trigger
        //   // events like expanding SubComponents and pivots.
        //   // By default a custom 'onClick' handler will override this functionality.
        //   // If you want to fire the original onClick handler, call the
        //   // 'handleOriginal' function.
        //   if (handleOriginal) {
        //     handleOriginal();
        //   }
        // },
        // style: {
        //   background: rowInfo && selection.includes(`select-${rowInfo.original.id}`) && 'lightgreen'
        // }
        className: rowInfo && selection.includes(`select-${rowInfo.original.id}`) ? 'has-background-grey-lighter' : ''
      };
    }
    return {};
  };

  render() {
    const columns: Column[] = [
      { Header: 'ID', accessor: 'id', maxWidth: 100, className: 'has-text-centered' },
      { Header: 'First Name', accessor: 'firstName', minWidth: 200, Cell: this.renderEditable },
      { Header: 'Last Name', accessor: 'lastName', minWidth: 200, Cell: this.renderEditable },
      { Header: 'Age', accessor: 'age', maxWidth: 100, className: 'has-text-centered' }
    ];

    return (
      <SelectTable
        {...selectTableAdditionalProps}
        data={this.state.data}
        columns={columns}
        // normal ref won't work since we need the wrapped instance
        // therefore use an arrow function to set the ref
        ref={(ref) => {
          this.selectTable = ref;
        }}
        toggleSelection={this.toggleSelection}
        selectAll={this.state.selectAll}
        toggleAll={this.toggleAll}
        isSelected={this.isSelected}
        getTrProps={this.rowFn}
        className="-striped -highlight has-text-left"
      />
    );
  }
}
