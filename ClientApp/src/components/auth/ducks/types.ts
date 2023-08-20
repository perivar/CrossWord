// ducks/types.ts
// This file contains string literals for your action types.
// This provides an easy reference for the actions available.
// These strings are exported as an object literal which can then be imported into your reducers and action creators instead of hard-coding them.

import { Action } from 'redux';
import { IUser, ILogon } from '../types';

export enum UserActionTypes {
  REGISTER_REQUEST = 'USERS_REGISTER_REQUEST',
  REGISTER_SUCCESS = 'USERS_REGISTER_SUCCESS',
  REGISTER_FAILURE = 'USERS_REGISTER_FAILURE',

  LOGIN_REQUEST = 'USERS_LOGIN_REQUEST',
  LOGIN_SUCCESS = 'USERS_LOGIN_SUCCESS',
  LOGIN_FAILURE = 'USERS_LOGIN_FAILURE',

  LOGOUT = 'USERS_LOGOUT',

  INVALID_TOKEN = 'INVALID_TOKEN',
  REFRESHING_TOKEN = 'REFRESHING_TOKEN',
  TOKEN_REFRESHED = 'TOKEN_REFRESHED',
  SAVE_TOKENS = 'SAVE_TOKENS',

  GETALL_REQUEST = 'USERS_GETALL_REQUEST',
  GETALL_SUCCESS = 'USERS_GETALL_SUCCESS',
  GETALL_FAILURE = 'USERS_GETALL_FAILURE',

  DELETE_REQUEST = 'USERS_DELETE_REQUEST',
  DELETE_SUCCESS = 'USERS_DELETE_SUCCESS',
  DELETE_FAILURE = 'USERS_DELETE_FAILURE'
}

// login(username: string, password: string)
interface LoginRequestAction extends Action {
  type: typeof UserActionTypes.LOGIN_REQUEST;
  username: string;
}

interface LoginSuccessAction extends Action {
  type: typeof UserActionTypes.LOGIN_SUCCESS;
  user: ILogon;
}

interface LoginFailureAction extends Action {
  type: typeof UserActionTypes.LOGIN_FAILURE;
  error: string;
}

// logout()
interface LogoutAction extends Action {
  type: typeof UserActionTypes.LOGOUT;
}

// token
interface InvalidTokenAction extends Action {
  type: typeof UserActionTypes.INVALID_TOKEN;
}

interface RefreshingTokenAction extends Action {
  type: typeof UserActionTypes.REFRESHING_TOKEN;
}

interface TokenRefreshedAction extends Action {
  type: typeof UserActionTypes.TOKEN_REFRESHED;
}

interface SaveTokensAction extends Action {
  type: typeof UserActionTypes.SAVE_TOKENS;
  token: string;
  refreshToken: string;
}

// register(user: IUser)
interface RegisterRequestAction extends Action {
  type: typeof UserActionTypes.REGISTER_REQUEST;
  user: IUser;
}

interface RegisterSuccessAction extends Action {
  type: typeof UserActionTypes.REGISTER_SUCCESS;
  user: IUser;
}

interface RegisterFailureAction extends Action {
  type: typeof UserActionTypes.REGISTER_FAILURE;
  error: string;
}

// getAll() {
interface GetAllRequestAction extends Action {
  type: typeof UserActionTypes.GETALL_REQUEST;
}

interface GetAllSuccessAction extends Action {
  type: typeof UserActionTypes.GETALL_SUCCESS;
  users: IUser[];
}

interface GetAllFailureAction extends Action {
  type: typeof UserActionTypes.GETALL_FAILURE;
  error: string;
}

// delete(username: string)
interface DeleteRequestAction extends Action {
  type: typeof UserActionTypes.DELETE_REQUEST;
  username: string;
  id: string;
}

interface DeleteSuccessAction extends Action {
  type: typeof UserActionTypes.DELETE_SUCCESS;
  username: string;
  id: string;
}

interface DeleteFailureAction extends Action {
  type: typeof UserActionTypes.DELETE_FAILURE;
  username: string;
  id: string;
  error: string;
}

export type UserActions =
  | LoginRequestAction
  | LoginSuccessAction
  | LoginFailureAction
  | LogoutAction
  | InvalidTokenAction
  | RefreshingTokenAction
  | TokenRefreshedAction
  | SaveTokensAction
  | RegisterRequestAction
  | RegisterSuccessAction
  | RegisterFailureAction
  | GetAllRequestAction
  | GetAllSuccessAction
  | GetAllFailureAction
  | DeleteRequestAction
  | DeleteSuccessAction
  | DeleteFailureAction;
