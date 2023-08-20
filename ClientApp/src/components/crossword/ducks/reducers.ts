/* eslint-disable default-param-last */
// ducks/reducers.ts
// The reducer deals with updating the state.

import { CrosswordActions, CrosswordActionTypes } from './types';
import { ICrosswordContainerState } from '../types';

const initialCrosswordState: ICrosswordContainerState = {
  loading: false,
  data: {
    id: '',
    number: 0,
    name: '',
    creator: { name: '', webUrl: '' },
    date: 0,
    entries: [],
    solutionAvailable: false,
    dateSolutionAvailable: 0,
    dimensions: { rows: 0, cols: 0 },
    crosswordType: 'quick',
    pdf: ''
  },
  error: ''
};

const crosswordReducer = function crossword(
  state = initialCrosswordState,
  action: CrosswordActions
): ICrosswordContainerState {
  switch (action.type) {
    case CrosswordActionTypes.GET_REQUEST:
      return {
        ...state,
        loading: true
      };
    case CrosswordActionTypes.GET_SUCCESS:
      return {
        ...state,
        loading: false,
        data: action.data
      };
    case CrosswordActionTypes.GET_FAILURE:
      return {
        ...state,
        loading: false,
        error: action.error
      };
    default:
      return state;
  }
};

export default {
  crossword: crosswordReducer
};
