/* eslint-disable @typescript-eslint/no-shadow */
import React, {
  Dispatch,
  SetStateAction,
  ChangeEvent,
  CSSProperties,
  HTMLAttributes,
  useCallback,
  useState,
  useEffect,
  useRef,
  ReactNode
} from 'react';
import axios, { AxiosInstance } from 'axios';
import produce, { Draft } from 'immer';
import { useDataApi } from '../hooks/data-api-hook';
import './bulma-table.scss';
import { BulmaCheckboxField } from './BulmaCheckboxField';
import BulmaPaginator, { PaginationPlacement } from './BulmaPagination';
import { BulmaSearchField } from './BulmaSearchField';
// import { useWhyDidYouUpdate } from '../hooks/why-did-you-update-hook';
// import { useDependenciesDebugger } from '../hooks/dependency-debugger-hook';

export type SortingType = 'desc' | 'asc' | 'both';

export interface SortableTableData {
  [key: string]: any;
}

export interface SortableCheckboxMap {
  [key: string]: boolean;
}

export interface SortableTableColumn {
  header: string;
  key: string;
  uniqueId?: boolean; // which column holds the id key
  defaultSorting?: string; // ASC or asc or DESC or desc
  headerStyle?: CSSProperties; // { fontSize: '15px', backgroundColor: '#FFDAB9', width: '100px' },
  headerProps?: HTMLAttributes<HTMLElement>; // { className: 'align-left' },
  dataStyle?: CSSProperties; // { fontSize: '15px', backgroundColor: '#FFDAB9' },
  dataProps?: HTMLAttributes<HTMLElement>; // { className: 'align-right' },
  sortable?: boolean;
  searchable?: boolean;
  render?: (value: RenderProps) => ReactNode;
  descSortFunction?: (sortedData: SortableTableData, key: string) => SortableTableData;
  ascSortFunction?: (sortedData: SortableTableData, key: string) => SortableTableData;
}

export interface SortableTableState {
  sortings: SortingType[];
  isAllSelected: boolean;
  checkboxes: SortableCheckboxMap;
  filter: string;
}

export interface RenderProps {
  uniqueRowId: string;
  column: SortableTableColumn;
  row: any;
  value: any;
  url?: string;
  setUrl: Function;
  setTableState: Dispatch<SetStateAction<SortableTableState>>;
}

export interface SortableTableIconInfo {
  iconStyle?: CSSProperties;
  iconDesc?: ReactNode;
  iconAsc?: ReactNode;
  iconBoth?: ReactNode;
}

export interface QueryParams {
  limit: number;
  offset: number;
  search: string;
  sort: string;
  order: SortingType;
  [key: string]: string | number;
}

export interface ResponseParams {
  total: number;
  rows: any;
}

export interface ActionButtonProps {
  tableState: SortableTableState;
  setTableState: Dispatch<SetStateAction<SortableTableState>>;
  url?: string;
  setUrl: Function;
}

export interface ActionButton {
  key: string;
  render?: (value: ActionButtonProps) => ReactNode;
}

export interface SortableTableProps extends SortableTableIconInfo {
  columns: SortableTableColumn[];
  data: SortableTableData;
  setData?: Dispatch<React.SetStateAction<SortableTableData | any>>;
  tableState: SortableTableState;
  setTableState: Dispatch<SetStateAction<SortableTableState>>;
  pagination?: boolean;
  search?: boolean;
  pageSize?: number;
  baseUrl?: string;
  url?: string;
  sidePagination?: 'server' | 'client';
  sortOrder?: 'desc' | 'asc';
  queryParams?: (params: QueryParams) => Record<string, string | number | boolean | undefined>;
  responseHandler?: (response: any) => ResponseParams;
  style?: CSSProperties;
  maxButtons?: number;
  paginationPlacement?: PaginationPlacement;
  useGotoField?: boolean;
  isSelectable?: boolean;
  alwaysUsePreviousNextButtons?: boolean;
  actionButtons?: ActionButton[];
  onAll?: (type: string, param?: any) => void;
  onSort?: (sortings: any) => void;
  onCheck?: (id: string) => void;
  onUncheck?: (id: string) => void;
  onCheckAll?: () => void;
  onUncheckAll?: () => void;
  onLoadSuccess?: (data: any, totalCount: number) => void;
  onLoadError?: (error: any) => void;
  onPageChange?: (pageNumber: number) => void;
  onSearch?: (query: string) => void;
  previousText?: string;
  nextText?: string;
  rowsPerPageText?: string;
  renderShowing?: (fromRow: number, toRow: number, numberOfRows: number) => React.ReactNode;
  findInText?: string;
  searchText?: string;
  elementsText?: string;
  renderNumberOfRows?: (numberOfRows: number, tableState: SortableTableState) => React.ReactNode;
  notFound?: string;
}

interface SortableTableHeaderProps extends SortableTableIconInfo {
  columns: SortableTableColumn[];
  tableState: SortableTableState;
  setTableState: Dispatch<SetStateAction<SortableTableState>>;
  handleCheckboxChange: (changeEvent: ChangeEvent<HTMLInputElement>) => void;
  isSelectable: boolean;
}

interface SortableTableHeaderItemProps extends SortableTableIconInfo {
  column: SortableTableColumn;
  setTableState: Dispatch<SetStateAction<SortableTableState>>;
  index: number;
  sorting?: SortingType;
}

// SortableTableHeaderItem
function SortableTableHeaderItem(props: SortableTableHeaderItemProps) {
  // useWhyDidYouUpdate('SortableTableHeaderItem', props);
  const { column, setTableState, index, sorting, iconStyle, iconDesc, iconAsc, iconBoth } = props;
  // console.log(`render header :: ${column.key}`);
  const handleHeaderClick = useCallback(() => {
    if (column.sortable || column.sortable === undefined) {
      setTableState(
        produce((draft: Draft<SortableTableState>) => {
          draft.sortings = draft.sortings.map((sorting: SortingType, i: number) => {
            // set next sorting type for the selected sorting
            // the others need to be reset back to both
            if (i === index) {
              sorting = nextSortingState(sorting);
            } else {
              sorting = 'both';
            }
            return sorting;
          });
        })
      );
    }
  }, [column.sortable, index, setTableState]);

  // default to being sortable (i.e. undefined)
  let sortIcon;
  if (column.sortable || column.sortable === undefined) {
    if (iconBoth) {
      sortIcon = iconBoth;
    } else {
      sortIcon = (
        <span className="icon has-text-grey-light" style={iconStyle}>
          <i className="fas fa-sort" />
        </span>
      );
    }
    if (sorting === 'desc') {
      if (iconDesc) {
        sortIcon = iconDesc;
      } else {
        sortIcon = (
          <span className="icon has-text-info" style={iconStyle}>
            <i className="fas fa-sort-up" />
          </span>
        );
      }
    } else if (sorting === 'asc') {
      if (iconAsc) {
        sortIcon = iconAsc;
      } else {
        sortIcon = (
          <span className="icon has-text-info" style={iconStyle}>
            <i className="fas fa-sort-down" />
          </span>
        );
      }
    }
  }

  return (
    <th
      className="is-unselectable"
      key={`header-${column.key}`}
      style={column.headerStyle}
      onClick={handleHeaderClick}
      {...column.headerProps}>
      {column.header}
      {sortIcon}
    </th>
  );
}

const MemoizedSortableTableHeaderItem = React.memo(SortableTableHeaderItem);

// SortableTableHeader
function SortableTableHeader(props: SortableTableHeaderProps) {
  // useWhyDidYouUpdate('SortableTableHeader', props);
  const {
    columns,
    tableState,
    setTableState,
    iconStyle,
    iconDesc,
    iconAsc,
    iconBoth,
    handleCheckboxChange,
    isSelectable
  } = props;
  const headers = columns.map((column: SortableTableColumn, index: number) => {
    const sorting = tableState.sortings[index];

    const sortableTableHeaderItem = (
      <MemoizedSortableTableHeaderItem
        key={`header-${column.key}`}
        column={column}
        // tableState={tableState}
        setTableState={setTableState}
        index={index}
        sorting={sorting}
        iconStyle={iconStyle}
        iconDesc={iconDesc}
        iconAsc={iconAsc}
        iconBoth={iconBoth}
      />
    );

    return sortableTableHeaderItem;
  });

  const name = 'checkAll';
  const checkbox = (
    <th key="header-select-all">
      <BulmaCheckboxField
        label=""
        name={name}
        checked={tableState.isAllSelected}
        handleChange={handleCheckboxChange}
        checkboxProps={{ 'aria-label': `${tableState.isAllSelected ? 'Un-select all' : 'Select all'}` }}
      />
    </th>
  );

  return (
    <thead>
      <tr key="header-row">
        {isSelectable && checkbox}
        {headers}
      </tr>
    </thead>
  );
}

interface SortableTableRowProps {
  data: SortableTableData;
  columns: SortableTableColumn[];
  uniqueIdKey: string;
  isSelected: boolean;
  url?: string;
  setUrl: Function;
  setTableState: Dispatch<SetStateAction<SortableTableState>>;
  handleCheckboxChange: (changeEvent: ChangeEvent<HTMLInputElement>) => void;
  isSelectable: boolean;
}

// SortableTableRow
function SortableTableRow(props: SortableTableRowProps) {
  // useWhyDidYouUpdate('SortableTableRow', props);
  const { data, columns, uniqueIdKey, isSelected, url, setUrl, setTableState, handleCheckboxChange, isSelectable } =
    props;

  // console.log(`render row :: ${data[uniqueIdKey]}`);

  const tds = columns.map((column: SortableTableColumn) => {
    let value = data[column.key];
    if (column.render) {
      const renderInfo: RenderProps = {
        uniqueRowId: data[uniqueIdKey],
        column,
        value,
        row: data,
        url,
        setUrl,
        setTableState
      };
      value = column.render(renderInfo);
    }
    return (
      <td
        key={`row-${data[uniqueIdKey]}-${column.key}`}
        data-label={column.header}
        style={column.dataStyle}
        {...(column.dataProps || {})}>
        {value}
      </td>
    );
  });

  const checkbox = (
    <td key={`row-${data[uniqueIdKey]}-select`}>
      <BulmaCheckboxField
        label=""
        name={`${data[uniqueIdKey]}`}
        checked={isSelected}
        handleChange={handleCheckboxChange}
        checkboxProps={{ 'aria-label': `${isSelected ? 'Un-select' : 'Select'} row with id: ${data[uniqueIdKey]}` }}
      />
    </td>
  );

  return (
    <tr key={`row-${data[uniqueIdKey]}`} className={isSelected ? 'is-selected' : undefined}>
      {isSelectable && checkbox}
      {tds}
    </tr>
  );
}

interface SortableTableBodyProps {
  columns: SortableTableColumn[];
  uniqueIdKey: string;
  data: SortableTableData;
  tableState: SortableTableState;
  setTableState: Dispatch<SetStateAction<SortableTableState>>;
  url?: string;
  setUrl: Function;
  handleCheckboxChange: (changeEvent: ChangeEvent<HTMLInputElement>) => void;
  notFound?: string;
  isSelectable: boolean;
}

// make sure to memoize the rows to avoid re-renders
const MemoizedSortableTableRow = React.memo(SortableTableRow);

// SortableTableBody
function SortableTableBody(props: SortableTableBodyProps) {
  // useWhyDidYouUpdate('SortableTableBody', props);
  const {
    columns,
    uniqueIdKey,
    data,
    tableState,
    setTableState,
    url,
    setUrl,
    handleCheckboxChange,
    notFound,
    isSelectable
  } = props;

  const bodies = data.map((row: any) => {
    const sortableTableRow = (
      <MemoizedSortableTableRow
        key={`row-${row[uniqueIdKey]}`}
        uniqueIdKey={uniqueIdKey}
        data={row}
        columns={columns}
        isSelected={tableState.checkboxes[row[uniqueIdKey]]}
        handleCheckboxChange={handleCheckboxChange}
        url={url}
        setUrl={setUrl}
        setTableState={setTableState}
        isSelectable={isSelectable}
      />
    );

    return sortableTableRow;
  });

  return (
    <tbody>
      {data.length > 0 ? (
        bodies
      ) : (
        <tr>
          <td colSpan={columns.length + 1} className="has-text-centered">
            {notFound || 'No matching records found'}
          </td>
        </tr>
      )}
    </tbody>
  );
}

// filter
const filterData = (
  data: SortableTableData,
  columns: SortableTableColumn[],
  searchQuery: string
): SortableTableData => {
  // https://dev.to/iam_timsmith/lets-build-a-search-bar-in-react-120j
  let currentList: SortableTableData = [];

  // Variable to hold the filtered list before putting into state
  let newList: SortableTableData = [];

  // If the search bar isn't empty
  if (searchQuery !== '') {
    // Variable to hold the original version of the list
    currentList = data;

    // change search term to lowercase
    const lowercasedFilter = searchQuery.toLowerCase();

    // Use .filter() to determine which items should be displayed
    // based on the search terms
    newList = currentList.filter((item: any) => {
      return Object.keys(item).some((key) => {
        // lookup columns with this key
        const column = columns.find((a) => a.key === key);
        // check if the column has searchable set to true true or undefined
        if (column && (column.searchable || column.searchable === undefined)) {
          // get value using key
          const value = item[key];
          if (value) {
            let lowercasedValue = value;
            if (typeof value === 'string') {
              // change current item to lowercase
              lowercasedValue = value.toLowerCase();
            } else if (typeof value === 'number') {
              // change current item to string
              lowercasedValue = value.toString();
            }
            // check to see if the current list item includes the search term
            // If it does, it will be added to newList. Using lowercase eliminates
            // issues with capitalization in search terms and search content
            return lowercasedValue.includes(lowercasedFilter);
          }
          return null;
        }
        return null;
      });
    });
  } else {
    // If the search bar is empty, set newList to original task list
    newList = data;
  }

  return newList;
};

interface SortableTableTopBarProps {
  numberOfRows: number;
  tableState: SortableTableState;
  setTableState: Dispatch<SetStateAction<SortableTableState>>;
  search: boolean;
  url?: string;
  setUrl: Function;
  actionButtons?: ActionButton[];
  findInText?: string;
  searchText?: string;
  elementsText?: string;
  renderNumberOfRows?: (numberOfRows: number, tableState: SortableTableState) => React.ReactNode;
}

// SortableTableSearchBar
function SortableTableTopBar(props: SortableTableTopBarProps) {
  const {
    numberOfRows,
    tableState,
    search,
    setTableState,
    url,
    setUrl,
    actionButtons,
    findInText = 'Find in table',
    searchText = 'Search',
    elementsText = 'elements',
    renderNumberOfRows
  } = props;

  const handleSearchSubmit = (filterQuery: string) => {
    setTableState(
      produce((draft: Draft<SortableTableState>) => {
        draft.filter = filterQuery;
      })
    );
  };

  const actionInfo: ActionButtonProps = { tableState, setTableState, url, setUrl };

  return (
    <nav className="level">
      <div className="level-left">
        <div className="level-item">
          <div className="buttons is-centered">
            {actionButtons &&
              actionButtons.map((actionButton: ActionButton) => (
                <div key={actionButton.key}>{actionButton.render && actionButton.render(actionInfo)}</div>
              ))}
          </div>
        </div>
        <div className="level-item">
          {renderNumberOfRows ? (
            renderNumberOfRows(numberOfRows, tableState)
          ) : (
            <p>
              <strong>{numberOfRows}</strong> {elementsText}
            </p>
          )}
        </div>
      </div>
      <div className="level-right">
        <div className="level-item">
          {search && (
            <BulmaSearchField
              type="addon"
              label={searchText}
              value={tableState.filter}
              placeholder={findInText}
              handleSubmit={handleSearchSubmit}
            />
          )}
        </div>
      </div>
    </nav>
  );
}

// sort methods
const parseFloatable = (value: any): boolean => {
  return !!(typeof value === 'string' && (/^\d+$/.test(value) || /^\d+$/.test(value.replace(/[,.%$]/g, ''))));
};

const parseIfFloat = (value: any): number => {
  return parseFloat(value.replace(/,/g, ''));
};

const ascSortData = (data: SortableTableData, key: string): SortableTableData => {
  return sortDataByKey(data, key, (a: any, b: any) => {
    if (parseFloatable(a) && parseFloatable(b)) {
      a = parseIfFloat(a);
      b = parseIfFloat(b);
    }
    if (a >= b) {
      return 1;
    }
    if (a < b) {
      return -1;
    }
    return 0;
  });
};

const descSortData = (data: SortableTableData, key: string): SortableTableData => {
  return sortDataByKey(data, key, (a: any, b: any) => {
    if (parseFloatable(a) && parseFloatable(b)) {
      a = parseIfFloat(a);
      b = parseIfFloat(b);
    }
    if (a <= b) {
      return 1;
    }
    if (a > b) {
      return -1;
    }
    return 0;
  });
};

const sortData = (data: SortableTableData, columns: SortableTableColumn[], sortings: string[]): SortableTableData => {
  let sortedData = data;
  for (let i = 0; i < sortings.length; i++) {
    const sorting = sortings[i];
    const column = columns[i];
    const { key } = columns[i];
    switch (sorting) {
      case 'desc':
        if (column.descSortFunction && typeof column.descSortFunction === 'function') {
          sortedData = column.descSortFunction(sortedData, key);
        } else {
          sortedData = descSortData(sortedData, key);
        }
        break;
      case 'asc':
        if (column.ascSortFunction && typeof column.ascSortFunction === 'function') {
          sortedData = column.ascSortFunction(sortedData, key);
        } else {
          sortedData = ascSortData(sortedData, key);
        }
        break;

      // no default
    }
  }
  return sortedData;
};

const sortDataByKey = (data: SortableTableData, key: string, fn: any): SortableTableData => {
  // const clone = Object.assign([], data);
  const clone = [...(data as any)];

  return clone.sort((a: any, b: any) => {
    return fn(a[key], b[key]);
  });
};

// get state methods
const nextSortingState = (state: SortingType): SortingType => {
  let next;
  switch (state) {
    case 'both':
      next = 'desc';
      break;
    case 'desc':
      next = 'asc';
      break;
    case 'asc':
    default:
      next = 'both';
      break;
  }
  return next as SortingType;
};

export const getInitialSortings = (columns: SortableTableColumn[]): SortingType[] => {
  // console.log('getting initial sortings');
  const sortings = columns.map((column: SortableTableColumn) => {
    let sorting = 'both';
    if (column.defaultSorting) {
      const defaultSorting = column.defaultSorting.toLowerCase();

      if (defaultSorting === 'desc') {
        sorting = 'desc';
      } else if (defaultSorting === 'asc') {
        sorting = 'asc';
      }
    }
    return sorting as SortingType;
  });

  return sortings;
};

const getInitialCheckboxes = (data: SortableTableData, uniqueIdKey: string): SortableCheckboxMap => {
  // console.log('getting initial checkboxes');
  // the reduce function creates a map of ids and a boolean, initially false
  const checkboxes = data.reduce(
    (options: any, option: any) => ({
      ...options,
      [option[uniqueIdKey]]: false
    }),
    {}
  );
  return checkboxes;
};

const getCurrentDataSlice = (data: SortableTableData, activePage: number, rowsPerPage: number): SortableTableData => {
  // console.log('getting data slice. activePage: ' + activePage + ' ,rowsPerPage:' + rowsPerPage);
  const currentDataSlice = data.slice((activePage - 1) * rowsPerPage, activePage * rowsPerPage);
  return currentDataSlice;
};

// const getInitalTableState = (
//   data: SortableTableData,
//   columns: SortableTableColumn[],
//   uniqueIdKey: string
// ): SortableTableState => {
//   // console.log('getting initial table state');
//   return {
//     sortings: getInitialSortings(columns),
//     isAllSelected: false,
//     checkboxes: getInitialCheckboxes(data, uniqueIdKey),
//     filter: ''
//   };
// };

// make a new axios instance, so that we don’t pollute the global axios object
const axiosInstance: AxiosInstance = axios.create({});

// https://stackoverflow.com/questions/53446020/how-to-compare-oldvalues-and-newvalues-on-react-hooks-useeffect
// function usePrevious(value: any) {
//   const ref = useRef();
//   useEffect(() => {
//     ref.current = value;
//   });
//   return ref.current;
// }

// BulmaTable
function BulmaTable(props: SortableTableProps) {
  // useWhyDidYouUpdate('BulmaTable', props);
  const {
    columns,
    data,
    setData,
    tableState,
    setTableState,
    pagination = true,
    search = true,
    pageSize = 10,
    baseUrl: initialBaseUrl,
    url: initialUrl,
    sidePagination = 'client',
    sortOrder = 'asc',
    queryParams,
    responseHandler,
    style,
    maxButtons,
    paginationPlacement,
    useGotoField = true,
    isSelectable = true,
    alwaysUsePreviousNextButtons = true,
    actionButtons,
    onAll,
    onSort,
    onCheck,
    onUncheck,
    onCheckAll,
    onUncheckAll,
    onLoadSuccess,
    onLoadError,
    onPageChange,
    onSearch,
    iconStyle,
    iconDesc,
    iconAsc,
    iconBoth,
    previousText,
    nextText,
    rowsPerPageText,
    renderShowing,
    findInText,
    searchText,
    elementsText,
    renderNumberOfRows,
    notFound
  } = props;

  // unique id column key - default is 'id'
  const [uniqueIdKey, setUniqueIdKey] = useState('id');

  // don't need to initialize the current data chunk since we are running an effect that does the same
  const [currentData, setCurrentData] = useState<SortableTableData>([]);

  // paging state
  const [activePage, setActivePage] = useState(1);
  const [rowsPerPage, setRowsPerPage] = useState(pageSize);
  const [numberOfRows, setNumberOfRows] = useState(0);

  // data api for reading data over ODATA
  // instead of using the callback in the data api hook we can use the useEffect hook to monitor the response object
  const [url, setUrl] = useState(initialUrl);
  if (initialBaseUrl) axiosInstance.defaults.baseURL = initialBaseUrl;
  const {
    response,
    error,
    isLoading,
    setUrl: fetchData
  } = useDataApi({
    // isError
    axios: axiosInstance
  });

  // useEffect means run on intial rendering and each subsequent change to the dependency array

  useEffect(() => {
    // console.log('useEffect() - updating rowsPerPage using pageSize');
    setRowsPerPage(pageSize);
  }, [pageSize]);

  useEffect(() => {
    if (columns.length > 0) {
      // console.log('useEffect() - initializing uniqueIdColumn');

      // see if one of the columns have specified an unique id key
      const uniqueIdColumn = columns.find((a) => a.uniqueId === true);
      if (uniqueIdColumn) {
        setUniqueIdKey(uniqueIdColumn.key);
      }
    }
  }, [columns]);

  const prevInitialUrlRef = useRef<string | undefined>();
  useEffect(() => {
    // console.log('useEffect() - checking if we need to set url to new initialUrl');

    // check if url has changed
    const hasInitialUrlChanged = prevInitialUrlRef.current !== initialUrl;

    if (hasInitialUrlChanged) {
      console.log(`useEffect() - setting url to new initialUrl: ${initialUrl}`);
      setUrl(initialUrl);
    }

    prevInitialUrlRef.current = initialUrl;
  }, [initialUrl]);

  const prevUrlRef = useRef<string | undefined>();
  useEffect(() => {
    if (url) {
      // console.log('useEffect() - fetching data - checking if we need to do anything ...');

      // inline method to get full url
      const getFullUrl = () => {
        if (sidePagination === 'server') {
          const indexFound = tableState.sortings.findIndex((a) => a && a !== 'both');
          const index = indexFound !== -1 ? indexFound : 0; // default to first column
          const sort = columns[index].key;
          const order = indexFound !== -1 ? tableState.sortings[index] : sortOrder; // default to sortOrder
          const params: QueryParams = {
            limit: rowsPerPage,
            offset: (activePage - 1) * rowsPerPage,
            search: tableState.filter,
            sort,
            order
          };

          const queryObject = queryParams ? queryParams(params) : params;

          // have to reduce to remove any elements that are undefined
          const queryString = Object.entries(queryObject)
            .reduce((result, pair) => {
              const [key, value] = pair;
              if (value !== undefined && value !== null) {
                result.push(`${key}=${value}`);
              }
              return result;
            }, [] as string[])
            .join('&');
          return `${url}?${queryString}`;
        }
        return `${url}`;
      };

      // check if url has changed
      const hasUrlChanged = prevUrlRef.current !== url;
      let fullUrl = '';

      if (sidePagination === 'client') {
        if (!hasUrlChanged) {
          return;
        }
        // url has changed
        fullUrl = getFullUrl();
      } else {
        // server pagination
        fullUrl = getFullUrl();
      }

      // console.log('useEffect() - fetching data - fetching data using url: ' + fullUrl);
      fetchData(fullUrl);
    }

    prevUrlRef.current = url;
  }, [activePage, columns, rowsPerPage, sidePagination, sortOrder, url, tableState.sortings, tableState.filter]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (response) {
      // console.log('useEffect() - handling load success (response)');

      let localData = [];
      let localTotalCount = 0;
      if (responseHandler) {
        const { total, rows } = responseHandler(response.data);
        localData = rows;
        localTotalCount = total || rows.length;
      } else {
        localData = response.data.value;
        localTotalCount = response.data.value.length;
      }
      // console.log('useEffect() - handling response. Total count: ' + localTotalCount);
      if (onLoadSuccess) onLoadSuccess(localData, localTotalCount);
      if (onAll) onAll('onLoadSuccess', { localData, localTotalCount });

      setNumberOfRows(localTotalCount);

      if (setData) setData(localData);

      // check if we need to change the active page
      const localNumberOfPages = Math.ceil(localTotalCount / rowsPerPage);
      if (activePage > localNumberOfPages) {
        setActivePage(1); // 1 or localNumberOfPages?
      }
    }
  }, [response]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (error) {
      // console.log('useEffect() - handling load error');
      if (onLoadError) onLoadError(error);
      if (onAll) onAll('onLoadSuccess', error);
    }
  }, [error]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (data.length > 0) {
      // console.log('useEffect() - initializing table state for checkboxes');
      setTableState(
        produce((draft: Draft<SortableTableState>) => {
          draft.checkboxes = getInitialCheckboxes(data, uniqueIdKey);
        })
      );
    }
  }, [data, setTableState, uniqueIdKey]);

  useEffect(() => {
    if (data.length > 0 && tableState.sortings.length > 0) {
      if (sidePagination === 'server') {
        // console.log('useEffect() - server-side sorting and filtering');
        setCurrentData(data);
      } else {
        // console.log('useEffect() - local (client) sorting and filtering');
        const localSortedData = sortData(data, columns, tableState.sortings);

        const localSortedAndFilteredData =
          tableState.filter !== '' ? filterData(localSortedData, columns, tableState.filter) : localSortedData;

        // calculate number of rows from the possisly sorted and filtered data
        setNumberOfRows(localSortedAndFilteredData.length);

        if (localSortedAndFilteredData.length > rowsPerPage) {
          const localDataSlice = getCurrentDataSlice(localSortedAndFilteredData, activePage, rowsPerPage);
          setCurrentData(localDataSlice);
        } else {
          setCurrentData(localSortedAndFilteredData);
        }

        // check if we need to change the active page
        const localNumberOfPages = Math.ceil(localSortedAndFilteredData.length / rowsPerPage);
        if (activePage > localNumberOfPages) {
          setActivePage(1); // 1 or localNumberOfPages?
        }
      }
    } else {
      // no data
      setNumberOfRows(0);
      setCurrentData(data);
    }
  }, [activePage, columns, data, rowsPerPage, sidePagination, tableState.sortings, tableState.filter]); // eslint-disable-line react-hooks/exhaustive-deps

  // trigger events
  const prevPageRef = useRef<number>(1);
  useEffect(() => {
    // check if page has changed
    const hasPageChanged = prevPageRef.current !== activePage;

    if (hasPageChanged) {
      // console.log('useEffect() - changed page to: ' + activePage);
      if (onPageChange) onPageChange(activePage);
      if (onAll) onAll('onPageChange', activePage);
    }

    prevPageRef.current = activePage;
  }, [activePage]); // eslint-disable-line react-hooks/exhaustive-deps

  const prevSortingsRef = useRef<SortingType[]>(tableState.sortings);
  useEffect(() => {
    // console.log('useEffect() - handling changed tableState.sortings');

    // check if sortings has changed
    const hasSortingsChanged = prevSortingsRef.current !== tableState.sortings;

    if (hasSortingsChanged) {
      if (onSort) onSort(tableState.sortings);
      if (onAll) onAll('onSort', tableState.sortings);
    }

    prevSortingsRef.current = tableState.sortings;
  }, [tableState.sortings]); // eslint-disable-line react-hooks/exhaustive-deps

  const prevSearchFilterRef = useRef<string>('');
  useEffect(() => {
    // console.log('useEffect() - handling changed tableState.filter');

    // check if sortings has changed
    const hasSearchFilterChanged = prevSearchFilterRef.current !== tableState.filter;

    if (hasSearchFilterChanged) {
      if (onSearch) onSearch(tableState.filter);
      if (onAll) onAll('onSearch', tableState.filter);
    }

    prevSearchFilterRef.current = tableState.filter;
  }, [tableState.filter]); // eslint-disable-line react-hooks/exhaustive-deps

  // debug what has changed between renders
  // useDependenciesDebugger({ activePage, columns, data, rowsPerPage, sidePagination, sortOrder, url, tableState });

  const handleCheckboxChange = useCallback(
    (changeEvent: ChangeEvent<HTMLInputElement>) => {
      const { name: id } = changeEvent.target;

      if (id === 'checkAll') {
        setTableState(
          produce((draft: Draft<SortableTableState>) => {
            // toggle isAllSelected
            draft.isAllSelected = !draft.isAllSelected;

            // trigger events
            if (draft.isAllSelected) {
              if (onCheckAll) onCheckAll();
              if (onAll) onAll('onCheckAll');
            } else {
              if (onUncheckAll) onUncheckAll();
              if (onAll) onAll('onUncheckAll');
            }

            // and update all checkboxes
            Object.keys(draft.checkboxes).forEach((id) => {
              draft.checkboxes[id] = draft.isAllSelected;
            });
          })
        );
      } else {
        setTableState(
          produce((draft: Draft<SortableTableState>) => {
            // only toggle the given id
            draft.checkboxes[id] = !draft.checkboxes[id];

            // trigger events
            if (draft.checkboxes[id]) {
              if (onCheck) onCheck(id);
              if (onAll) onAll('onCheck', id);
            } else {
              if (onUncheck) onUncheck(id);
              if (onAll) onAll('onUncheck', id);
            }

            // check if all is selected
            draft.isAllSelected = Object.keys(draft.checkboxes).every((id) => draft.checkboxes[id]);
          })
        );
      }
    },
    [setTableState] // eslint-disable-line react-hooks/exhaustive-deps
  );

  const sortableTableHeader = SortableTableHeader({
    columns,
    tableState,
    setTableState,
    iconStyle,
    iconDesc,
    iconAsc,
    iconBoth,
    handleCheckboxChange,
    isSelectable
  });

  const sortableTableBody = SortableTableBody({
    columns,
    uniqueIdKey,
    data: currentData,
    tableState,
    setTableState,
    url,
    setUrl,
    handleCheckboxChange,
    notFound,
    isSelectable
  });

  const sortableTableTopBar = SortableTableTopBar({
    numberOfRows,
    tableState,
    setTableState,
    search,
    url,
    setUrl,
    actionButtons,
    findInText,
    searchText,
    elementsText,
    renderNumberOfRows
  });

  const bulmaPaginator = BulmaPaginator({
    initialPage: activePage,
    setInitialPage: setActivePage,
    numberOfRows,
    rowsPerPage,
    setRowsPerPage,
    maxButtons,
    paginationPlacement,
    useGotoField,
    alwaysUsePreviousNextButtons,
    previousText,
    nextText,
    rowsPerPageText,
    renderShowing
  });

  const indexFound = tableState.sortings.findIndex((a) => a && a !== 'both');
  const index = indexFound !== -1 ? indexFound : 0; // default to first column
  const sort = columns[index].key;
  const order = indexFound !== -1 ? tableState.sortings[index] : sortOrder; // default to sortOrder

  const handleMobileSortFieldChange = useCallback(
    (e: React.FocusEvent<HTMLSelectElement>) => {
      const index = e.target.selectedIndex;
      setTableState(
        produce((draft: Draft<SortableTableState>) => {
          draft.sortings = draft.sortings.map((sorting: SortingType, i: number) => {
            // set next sorting type for the selected sorting
            // the others need to be reset back to both
            if (i === index) {
              // default to sortOrder
              sorting = sortOrder;
            } else {
              sorting = 'both';
            }
            return sorting;
          });
        })
      );
    },
    [setTableState, sortOrder]
  );

  const handleMobileSortOrderClick = useCallback(() => {
    setTableState(
      produce((draft: Draft<SortableTableState>) => {
        // const indexFound = draft.sortings.findIndex(a => a && a !== 'both');
        // const index = indexFound !== -1 ? indexFound : 0; // default to first column

        draft.sortings = draft.sortings.map((sorting: SortingType, i: number) => {
          // set next sorting type for the selected sorting
          // the others need to be reset back to both
          if (i === index) {
            // sorting = nextSortingState(sorting);
            sorting = sorting === 'asc' ? 'desc' : 'asc';
          } else {
            sorting = 'both';
          }
          return sorting;
        });
      })
    );
  }, [index, setTableState]);

  return (
    <>
      {sortableTableTopBar}
      <div className="table-container">
        <div className="field table-mobile-sort">
          <div className="field has-addons">
            <div className="control is-expanded">
              <span className="select is-fullwidth">
                <select value={sort} onBlur={handleMobileSortFieldChange} onChange={handleMobileSortFieldChange}>
                  {columns.map((column: SortableTableColumn) =>
                    column.sortable || column.sortable === undefined ? (
                      <option key={column.key} value={column.key}>
                        {column.header}
                      </option>
                    ) : (
                      ''
                    )
                  )}
                </select>
              </span>
            </div>
            <div className="control">
              <button type="button" className="button is-primary" onClick={handleMobileSortOrderClick}>
                <span className={`icon is-small ${order === 'desc' ? 'is-desc' : ''}`}>
                  <i className="fas fa-arrow-up" />
                </span>
              </button>
            </div>
          </div>
        </div>
        <table className="table has-mobile-cards is-bordered is-striped is-hoverable is-fullwidth" style={style}>
          {sortableTableHeader}
          {sortableTableBody}
        </table>
      </div>
      {isLoading && <div className="is-loading" />}
      {pagination && bulmaPaginator}
    </>
  );
}

export default BulmaTable;
