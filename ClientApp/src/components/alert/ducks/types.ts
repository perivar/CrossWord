// ducks/types.ts
// This file contains string literals for your action types.
// This provides an easy reference for the actions available.
// These strings are exported as an object literal which can then be imported into your reducers and action creators instead of hard-coding them.

import { Action } from 'redux';

export enum AlertActionTypes {
  SUCCESS = 'ALERT_SUCCESS',
  ERROR = 'ALERT_ERROR',
  CLEAR = 'ALERT_CLEAR'
}

export interface IAlertSuccessAction extends Action {
  type: AlertActionTypes.SUCCESS;
  message: string;
}

export interface IAlertErrorAction extends Action {
  type: AlertActionTypes.ERROR;
  message: string;
}

export interface IAlertClearAction extends Action {
  type: AlertActionTypes.CLEAR;
}

/* 
Combine the action types with a union (we assume there are more)
example: export type CharacterActions = IGetAllAction | IGetOneAction ... 
*/
export type AlertActions = IAlertSuccessAction | IAlertErrorAction | IAlertClearAction;
