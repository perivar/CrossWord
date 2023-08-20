import { crosswordService } from './operations';
import { CrosswordActions, CrosswordActionTypes } from './types';
import * as alertActions from '../../alert/ducks/actions';

// ducks/actions.ts
// This is where you define your action creators.
// All action creators must be functions that return an object with at least the type property.
// We do not define any async logic in this file.

function get() {
  // action creators
  function request(): CrosswordActions {
    return { type: CrosswordActionTypes.GET_REQUEST };
  }
  function success(data: any): CrosswordActions {
    return { type: CrosswordActionTypes.GET_SUCCESS, data };
  }
  function failure(error: string): CrosswordActions {
    return { type: CrosswordActionTypes.GET_FAILURE, error };
  }

  return (dispatch: any) => {
    dispatch(request());

    crosswordService.get().then(
      (data: any) => {
        dispatch(success(data));
      },
      (error: any) => {
        dispatch(failure(error.toString()));
        dispatch(alertActions.error(error.toString()));
      }
    );
  };
}

export const crosswordActions = {
  get
};
