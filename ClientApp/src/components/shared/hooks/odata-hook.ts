import { useState } from 'react';
// import produce, { Draft } from 'immer';

export type FilterOperation =
  | 'contains'
  // | "notContains"
  // | "startsWith"
  // | "endsWith"
  | 'equal'
  // | "notEqual"
  | 'greaterThan'
  | 'greaterThanOrEqual'
  | 'lessThan';
// | "lessThanOrEqual"

export type DataType = 'number' | 'string' | 'guid';

export interface OdataFilter {
  name: string;
  operation: FilterOperation;
  value: string;
  dataType: DataType;
}

export interface OrderBy {
  name: string;
  direction: 'asc' | 'desc';
}

export interface OdataValues {
  top?: number;
  skip?: number;
  filters?: OdataFilter[] | undefined;
  orderBy?: OrderBy[] | undefined;
  count?: boolean;
}

const mapToOdataFilter = ({ name, operation: operand, value, dataType }: OdataFilter) => {
  switch (operand) {
    case 'contains': {
      return `contains(${name}, ${dataType === 'number' ? value : `'${value}'`})`;
    }
    case 'equal': {
      return `${name} eq ${dataType !== 'string' ? value : `'${value}'`}`;
    }
    case 'greaterThanOrEqual': {
      return `${name} ge ${dataType !== 'string' ? value : `'${value}'`}`;
    }
    case 'greaterThan': {
      return `${name} gt ${dataType !== 'string' ? value : `'${value}'`}`;
    }
    case 'lessThan': {
      return `${name} lt ${dataType !== 'string' ? value : `'${value}'`}`;
    }

    // no default
  }
  throw Error(`unknown operand '${operand}'`);
};

const getOdataOrderBy = (orderBy: OrderBy[] | undefined) => {
  const orderByPara = orderBy && orderBy.map((sort) => `${sort.name} ${sort.direction}`).join(',');
  return orderByPara;
};

const getOdataFilter = (filters: OdataFilter[] | undefined) => {
  const filter =
    filters && filters.length > 0 && `${filters.map(mapToOdataFilter).reduce((prev, curr) => `${prev} and ${curr}`)}`;

  return filter;
};

export const getOdataQuery = ({ top = 20, skip = 0, filters, orderBy, count = false }: OdataValues) => {
  const orderByPara = getOdataOrderBy(orderBy);
  const filter = getOdataFilter(filters);
  const orderByQuery = orderByPara ? `&$orderBy=${orderByPara}` : '';
  const queryString = `$top=${top}${skip ? `&$skip=${skip}` : ''}${
    filter ? `&$filter=${filter}` : ''
  }${orderByQuery}&$count=${count}`;

  return queryString;
};

export const getOdataQueryObject = ({ top = 20, skip = 0, filters, orderBy, count = false }: OdataValues) => {
  const orderByPara = getOdataOrderBy(orderBy);
  const filter = getOdataFilter(filters);

  const queryObject = {
    $top: top,
    $skip: skip,
    $filter: filter,
    $orderBy: orderByPara,
    $count: count
  };

  return queryObject;
};

export const useOdata = ({ top = 20, skip = 0, filters, orderBy, count = false }: OdataValues) => {
  const [state, setState] = useState<OdataValues>({ top, skip, filters, orderBy, count });

  const queryString = getOdataQuery({
    top: state.top,
    skip: state.skip,
    filters: state.filters,
    orderBy: state.orderBy,
    count: state.count
  });

  const queryObject = getOdataQueryObject({
    top: state.top,
    skip: state.skip,
    filters: state.filters,
    orderBy: state.orderBy,
    count: state.count
  });

  /*
  const setTop = (top: number) => {
    setState(
      produce((draft: Draft<OdataValues>) => {
        draft.top = top;
      })
    );
  };
  const setSkip = (skip: number) => {
    setState(
      produce((draft: Draft<OdataValues>) => {
        draft.skip = skip;
      })
    );
  };
  const setFilters = (filters: OdataFilter[]) => {
    setState(
      produce((draft: Draft<OdataValues>) => {
        draft.filters = filters;
      })
    );
  };
  const setOrderBy = (orderBy: OrderBy[]) => {
    setState(
      produce((draft: Draft<OdataValues>) => {
        draft.orderBy = orderBy;
      })
    );
	};
	*/

  // return { queryString, queryObject, state, setState, setTop, setSkip, setFilters, setOrderBy };
  return { queryString, queryObject, state, setState };
};
