// ducks/types.ts
// This file contains string literals for your action types.
// This provides an easy reference for the actions available.
// These strings are exported as an object literal which can then be imported into your reducers and action creators instead of hard-coding them.

import { Action } from 'redux';

export enum CrosswordActionTypes {
  GET_REQUEST = 'CROSSWORD_GET_REQUEST',
  GET_SUCCESS = 'CROSSWORD_GET_SUCCESS',
  GET_FAILURE = 'CROSSWORD_GET_FAILURE'
}

// getCrossword() {
interface GetCrosswordRequestAction extends Action {
  type: typeof CrosswordActionTypes.GET_REQUEST;
}

interface GetCrosswordSuccessAction extends Action {
  type: typeof CrosswordActionTypes.GET_SUCCESS;
  data: any;
}

interface GetCrosswordFailureAction extends Action {
  type: typeof CrosswordActionTypes.GET_FAILURE;
  error: string;
}

export type CrosswordActions = GetCrosswordRequestAction | GetCrosswordSuccessAction | GetCrosswordFailureAction;
