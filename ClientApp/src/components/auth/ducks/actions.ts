/* eslint-disable @typescript-eslint/no-shadow */
// ducks/actions.ts
// This is where you define your action creators.
// All action creators must be functions that return an object with at least the type property.
// We do not define any async logic in this file.

import { UserActionTypes, UserActions } from './types';
import { userService } from './operations';
import { IUser, ILogon } from '../types';
import { history } from '../../../history';
import * as alertActions from '../../alert/ducks/actions';

function login(username: string, password: string) {
  // action creators
  function request(username: string): UserActions {
    return { type: UserActionTypes.LOGIN_REQUEST, username };
  }
  function success(user: ILogon): UserActions {
    return { type: UserActionTypes.LOGIN_SUCCESS, user };
  }
  function failure(error: string): UserActions {
    return { type: UserActionTypes.LOGIN_FAILURE, error };
  }

  return (dispatch: any) => {
    dispatch(request(username));

    userService.login(username, password).then(
      (user: ILogon) => {
        dispatch(success(user));
        history.push('/');
      },
      (error: any) => {
        dispatch(failure(error.toString()));
        dispatch(alertActions.error(error.toString()));
      }
    );
  };
}

function logout(): UserActions {
  // userService.logout();
  return { type: UserActionTypes.LOGOUT };
}

function register(user: IUser) {
  // action creators
  function request(user: IUser): UserActions {
    return { type: UserActionTypes.REGISTER_REQUEST, user };
  }
  function success(user: IUser): UserActions {
    return { type: UserActionTypes.REGISTER_SUCCESS, user };
  }
  function failure(error: string): UserActions {
    return { type: UserActionTypes.REGISTER_FAILURE, error };
  }

  return (dispatch: any) => {
    dispatch(request(user));

    userService.register(user).then(
      (user) => {
        dispatch(success(user));
        history.push('/login');
        dispatch(alertActions.success('Registration successfull'));
      },
      (error) => {
        dispatch(failure(error.toString()));
        dispatch(alertActions.error(error.toString()));
      }
    );
  };
}

function getAll() {
  // action creators
  function request(): UserActions {
    return { type: UserActionTypes.GETALL_REQUEST };
  }
  function success(users: IUser[]): UserActions {
    return { type: UserActionTypes.GETALL_SUCCESS, users };
  }
  function failure(error: string): UserActions {
    return { type: UserActionTypes.GETALL_FAILURE, error };
  }

  return (dispatch: any) => {
    dispatch(request());

    userService.getAll().then(
      (users: any) => {
        dispatch(success(users));
      },
      (error: any) => {
        dispatch(failure(error.toString()));
        // dispatch(alertActions.error(error.toString()));
      }
    );
  };
}

// prefixed function name with underscore because delete is a reserved word in javascript
function _delete(username: string) {
  // action creators
  function request(username: string, id: string): UserActions {
    return { type: UserActionTypes.DELETE_REQUEST, username, id };
  }
  function success(username: string, id: string): UserActions {
    return { type: UserActionTypes.DELETE_SUCCESS, username, id };
  }
  function failure(username: string, id: string, error: string): UserActions {
    return { type: UserActionTypes.DELETE_FAILURE, username, id, error };
  }

  return (dispatch: any) => {
    dispatch(request(username, ''));

    userService.delete(username).then(
      (id: string) => {
        dispatch(success(username, id));
      },
      (error: any) => {
        dispatch(failure(username, '', error.toString()));
        dispatch(alertActions.error(error.toString()));
      }
    );
  };
}

const refreshToken = (token: string, refreshToken: string) => (dispatch: any) => {
  const success = (token: string, refreshToken: string) => {
    return {
      type: UserActionTypes.SAVE_TOKENS,
      token,
      refreshToken
    };
  };

  return userService
    .refreshToken(token, refreshToken)
    .then((tokens) => {
      dispatch(success(tokens.token, tokens.refreshToken));
    })
    .catch((error) => {
      dispatch(alertActions.error(error.toString()));
    });
};

export const userActions = {
  login,
  logout,
  register,
  getAll,
  delete: _delete,
  refreshToken
};
