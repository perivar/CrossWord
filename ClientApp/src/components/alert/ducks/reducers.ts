/* eslint-disable default-param-last */
// ducks/reducers.ts
// The reducer deals with updating the state.

import { AlertActionTypes, AlertActions } from './types';
import { IAlertState } from '../../auth/types';

const initialAlertState: IAlertState = {
  className: '',
  message: ''
};

const alertReducer = function alert(state = initialAlertState, action: AlertActions): IAlertState {
  switch (action.type) {
    case AlertActionTypes.SUCCESS:
      return {
        className: 'alert-success',
        message: action.message
      };
    case AlertActionTypes.ERROR:
      return {
        className: 'alert-danger',
        message: action.message
      };
    case AlertActionTypes.CLEAR:
      return {
        className: '',
        message: ''
      };
    default:
      return state;
  }
};

export default {
  alert: alertReducer
};
