import React, { useState } from 'react';
import produce, { Draft } from 'immer';
// import useRadioButtons from '../hooks/radio-buttons-hook';
import { getUniqueData, Data } from '../../dictionary/TableExampleData';
import './bulma-table.scss';
import BulmaTable, { SortableTableState, SortableTableColumn, ActionButton, getInitialSortings } from './BulmaTable';
import { PaginationPlacement } from './BulmaPagination';
import useRadioButtons from '../hooks/radio-buttons-hook';
import { BulmaConfirmButton } from './BulmaConfirmButton';

// render methods must have displayName
const renderIdUrl = (id: string) => {
  return <a href={`user/${id}`}>{id}</a>;
};
renderIdUrl.displayName = 'RenderIdUrl';

const columns: SortableTableColumn[] = [
  {
    header: 'Id',
    key: 'id'
    // defaultSorting: 'ASC'
    // headerStyle: { fontSize: '15px', backgroundColor: '#FFDAB9', width: '100px' },
    // dataStyle: { fontSize: '15px', backgroundColor: '#CCCCCC' },
    // dataProps: { className: 'align-right' },
    // render: renderIdUrl
  },
  {
    header: 'Age',
    key: 'age',
    // headerStyle: { fontSize: '20px' },
    sortable: false
  },
  {
    header: 'First Name',
    key: 'firstName',
    defaultSorting: 'ASC',
    // headerStyle: { fontSize: '15px' },
    // headerProps: { className: 'align-left' }
    searchable: false
  },
  {
    header: 'Last Name',
    key: 'lastName'
    // headerStyle: { fontSize: '15px' },
    // headerProps: { className: 'align-left' }
  }
];

const style = {
  // backgroundColor: '#eee'
};

const iconStyle = {
  // color: '#aaa',
  // paddingLeft: '5px',
  // paddingRight: '5px'
};

// initial table state
const intialState: SortableTableState = {
  sortings: getInitialSortings(columns),
  isAllSelected: false,
  checkboxes: {},
  filter: ''
};

export default function BulmaTableExample() {
  const [maxButtons, setMaxButtons] = useState(5);
  const [useGotoField, setUseGotoField] = useState<boolean>(false);
  const [isSelectable, setIsSelectable] = useState<boolean>(true);
  const [alwaysUsePreviousNextButtons, setAlwaysUsePreviousNextButtons] = useState<boolean>(false);
  const [pagination, setPagination] = useState<boolean>(true);
  const [search, setSearch] = useState<boolean>(true);
  const [pageSize, setPageSize] = useState<number>(5);

  // use radio button hook
  const { value: paginationPlacement, inputProps: paginationPlacementProps } = useRadioButtons<PaginationPlacement>(
    'paginationPlacement',
    'left'
  );

  const [data, setData] = useState<Data[]>(() => getUniqueData());

  const [tableState, setTableState] = useState<SortableTableState>(intialState);

  // create action buttons
  const handleOnDeleteClick = () => {
    const ids = Object.keys(tableState.checkboxes).filter((id) => tableState.checkboxes[id]);
    console.log(`delete: ${ids}`);
    setData(
      produce((draft: Draft<Data[]>) => {
        return draft.filter((element) => !ids.includes(element.id!.toString()));
      })
    );
  };

  const deleteButton: React.ReactNode = BulmaConfirmButton({
    type: 'danger',
    label: 'Delete',
    confirmLabel: 'Confirm delete',
    key: 'deleteRows',
    disabled: !Object.keys(tableState.checkboxes).some((id) => tableState.checkboxes[id]),
    handleOnClick: handleOnDeleteClick
  });

  // create action buttons renders
  const renderDeleteButton = (): React.ReactNode => {
    return deleteButton;
  };

  const actionButtons: ActionButton[] = [
    {
      key: 'deleteRows',
      render: renderDeleteButton
    }
  ];

  const bulmaTable = BulmaTable({
    columns,
    data,
    tableState,
    setTableState,
    pagination,
    search,
    pageSize,
    style,
    maxButtons,
    paginationPlacement,
    useGotoField,
    isSelectable,
    alwaysUsePreviousNextButtons,
    iconStyle,
    actionButtons,
    onAll: (type: string, param: any) => {
      console.log(`onAll: ${type} - ${param}`);
    },
    onSort: (sortings: any) => {
      console.log(`onSort: ${sortings}`);
    },
    onCheck: (id: string) => {
      console.log(`onCheck: ${id}`);
    },
    onUncheck: (id: string) => {
      console.log(`onUncheck: ${id}`);
    },
    onCheckAll: () => {
      console.log('onCheckAll');
    },
    onUncheckAll: () => {
      console.log('onUncheckAll');
    },
    onLoadSuccess: (_: any, totalCount: number) => {
      console.log(`onLoadSuccess: ${totalCount}`);
    },
    onLoadError: (error: any) => {
      console.log(`onLoadError: ${error}`);
    },
    onPageChange: (pageNumber: number) => {
      console.log(`onPageChange: ${pageNumber}`);
    },
    onSearch: (query: string) => {
      console.log(`onSearch: ${query}`);
    }
  });

  return (
    <>
      <div className="container box">
        <h1 className="is-size-5">BulmaTable demo</h1>
        <div>
          <div>
            Max buttons:&nbsp;
            <input
              type="range"
              value={maxButtons}
              min={1}
              max={10}
              onChange={(event) => setMaxButtons(Number(event.target.value))}
            />
            {maxButtons}
          </div>
        </div>
        <div>
          <div>
            <input
              type="checkbox"
              checked={alwaysUsePreviousNextButtons}
              onChange={() => setAlwaysUsePreviousNextButtons(!alwaysUsePreviousNextButtons)}
            />
            Always use previous & next buttons?
          </div>
        </div>
        <div>
          <div>
            <input type="checkbox" checked={useGotoField} onChange={() => setUseGotoField(!useGotoField)} />
            Use goto-field?
          </div>
        </div>
        <div>
          <div>
            <input type="checkbox" checked={isSelectable} onChange={() => setIsSelectable(!isSelectable)} />
            Is selectable?
          </div>
        </div>
        <div>
          <div>
            <input type="checkbox" checked={pagination} onChange={() => setPagination(!pagination)} />
            Use pagination?
          </div>
        </div>
        <div>
          <div>
            <input type="checkbox" checked={search} onChange={() => setSearch(!search)} />
            Use search?
          </div>
        </div>
        <div>
          <div>
            page size:&nbsp;
            <input
              type="range"
              value={pageSize}
              min={5}
              max={30}
              onChange={(event) => setPageSize(Number(event.target.value))}
            />
            {pageSize}
          </div>
        </div>
        <div>
          <div>Pagination placement:</div>
          <div>
            <label className="radio" htmlFor="left">
              <input value="left" id="left" checked={paginationPlacement === 'left'} {...paginationPlacementProps} />
              Left
            </label>
            <label className="radio" htmlFor="right">
              <input value="right" id="right" checked={paginationPlacement === 'right'} {...paginationPlacementProps} />
              Right
            </label>
            <label className="radio" htmlFor="centered">
              <input
                value="centered"
                id="centered"
                checked={paginationPlacement === 'centered'}
                {...paginationPlacementProps}
              />
              Centered
            </label>
            <label className="radio" htmlFor="inline">
              <input
                value="inline"
                id="inline"
                checked={paginationPlacement === 'inline'}
                {...paginationPlacementProps}
              />
              Inline
            </label>
          </div>
        </div>
      </div>
      {bulmaTable}
    </>
  );
}
