// ducks/operations.ts
// Here, we define any logic surrounding our actions and side effects, including async logic.
// If an action has no surrounding logic, then we simply forward them as is

import { IUser } from '../types';
import { store } from '../../../index';
import { IStoreState } from '../../../state/store';

const config = { apiUrl: process.env.REACT_APP_API };

// return authorization header with jwt token
function authHeader(): Headers {
  // const user = JSON.parse(localStorage.getItem('user') || '{}') as ILogon;

  // get redux store
  const theStore: IStoreState = store.getState();

  if (theStore.authentication && theStore.authentication.logon && theStore.authentication.logon.token) {
    return { Authorization: `Bearer ${theStore.authentication.logon.token}` } as any;
  }
  return {} as any;
}

// function logout() {
//   // remove user from local storage to log user out
//   // localStorage.removeItem('authentication.logon');
// }

function handleResponse(response: any) {
  return response.text().then((text: any) => {
    const data = text && JSON.parse(text);
    if (!response.ok) {
      const authenticate = response.headers.get('WWW-Authenticate');
      console.log(authenticate);

      // parse the error and error descrition
      // const regex = /(error|error_description)="(.+?)"/g;
      // let matches;
      // while ((matches = regex.exec(authenticate)) !== null) {
      //   matches.forEach((match, groupIndex) => {
      //     if (groupIndex === 1) console.log(`Param: ${match}`);
      //     if (groupIndex === 2) console.log(`Value: ${match}`);
      //   });
      // }

      const tokenExpired = response.headers.get('Token-Expired');
      const refreshExpired = response.headers.get('Refresh-Token-Expired');
      const invalidToken = response.headers.get('Invalid-Token');
      const invalidRefreshToken = response.headers.get('Invalid-Refresh-Token');

      if ((tokenExpired && tokenExpired === 'true') || (invalidToken && invalidToken === 'true')) {
        store.dispatch({ type: 'INVALID_TOKEN' });
      } else if (
        (refreshExpired && refreshExpired === 'true') ||
        (invalidRefreshToken && invalidRefreshToken === 'true')
      ) {
        store.dispatch({ type: 'REFRESH_EXPIRED' });
      }

      // extract error message and convert to string
      const error =
        (data && data.errors && data.errors.map((a: any) => a.description).join(' ')) ||
        (data && data.title) ||
        data ||
        response.statusText;
      return Promise.reject(error);
    }

    return data;
  });
}

function login(username: string, password: string) {
  const requestOptions: RequestInit = {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  };

  return fetch(`${config.apiUrl}/api/Account/Login`, requestOptions)
    .then(handleResponse)
    .then((user) => {
      // store user details and jwt token in local storage to keep user logged in between page refreshes
      // localStorage.setItem('user', JSON.stringify(user));
      // update - don't use localStorage directly - rather use redux with localStorage
      return user;
    });
}

function getAll() {
  const requestOptions: RequestInit = {
    method: 'GET',
    headers: authHeader()
  };

  return fetch(`${config.apiUrl}/api/Account/GetAll`, requestOptions).then(handleResponse);
}

function getByName(username: string) {
  const requestOptions: RequestInit = {
    method: 'GET',
    headers: authHeader()
  };

  return fetch(`${config.apiUrl}/api/Account/GetByName/${username}`, requestOptions).then(handleResponse);
}

function register(user: IUser) {
  const requestOptions: RequestInit = {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(user)
  };

  return fetch(`${config.apiUrl}/api/Account/Register`, requestOptions).then(handleResponse);
}

function update(user: IUser) {
  const requestOptions: RequestInit = {
    method: 'PUT',
    headers: { ...authHeader(), 'Content-Type': 'application/json' },
    body: JSON.stringify(user)
  };

  return fetch(`${config.apiUrl}/api/Account/Update/${user.username}`, requestOptions).then(handleResponse);
}

// prefixed function name with underscore because delete is a reserved word in javascript
function _delete(username: string) {
  const requestOptions: RequestInit = {
    method: 'DELETE',
    headers: authHeader()
  };

  return fetch(`${config.apiUrl}/api/Account/Delete/${username}`, requestOptions).then(handleResponse);
}

function refreshToken(token: string, refreshToken: string) {
  // const encodedToken = encodeURIComponent(token);
  // const encodedRefreshToken = encodeURIComponent(refreshToken);

  const requestOptions: RequestInit = {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    // body: JSON.stringify({ token: encodedToken, refreshToken: encodedRefreshToken })
    body: JSON.stringify({ token, refreshToken })
  };

  // note the refresh is a POST call but with query parameters
  return fetch(
    // `${config.apiUrl}/api/Account/RefreshAccessToken?token=${encodedToken}&refreshToken=${encodedRefreshToken}`,
    `${config.apiUrl}/api/Account/RefreshAccessToken`,
    requestOptions
  ).then(handleResponse);
}

export const userService = {
  login,
  register,
  getAll,
  getByName,
  update,
  delete: _delete,
  refreshToken
};
