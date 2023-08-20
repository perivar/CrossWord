// ducks/actions.ts
// This is where you define your action creators.
// All action creators must be functions that return an object with at least the type property.
// We do not define any async logic in this file.

import { AlertActionTypes, AlertActions } from './types';

// TypeScript infers that this function is returning IAlertSuccessAction
const success = (message: string): AlertActions => ({
  type: AlertActionTypes.SUCCESS,
  message
});

// TypeScript infers that this function is returning IAlertErrorAction
const error = (message: string): AlertActions => ({
  type: AlertActionTypes.ERROR,
  message
});

// TypeScript infers that this function is returning IAlertClearAction
const clear = (): AlertActions => ({
  type: AlertActionTypes.CLEAR
});

export { success, error, clear };
