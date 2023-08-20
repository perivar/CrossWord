// import { ILogon } from '../../auth/types';
import { store } from '../../../index';
import { IStoreState } from '../../../state/store';

// ducks/operations.ts
// Here, we define any logic surrounding our actions and side effects, including async logic.
// If an action has no surrounding logic, then we simply forward them as is

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

function handleResponse(response: any) {
  return response.text().then((text: any) => {
    const data = text && JSON.parse(text);
    if (!response.ok) {
      if (response.status === 401) {
        window.location.reload();
      }

      // extract error message and convert to sring
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

function get() {
  const requestOptions = {
    method: 'GET',
    headers: authHeader()
  };

  return fetch(`${config.apiUrl}/api/crosswordguardian`, requestOptions).then(handleResponse);
}

export const crosswordService = {
  get
};
