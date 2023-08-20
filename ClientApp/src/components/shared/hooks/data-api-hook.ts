import { useEffect, useReducer, useState, useRef } from 'react';
import defaultAxios, { AxiosInstance, AxiosStatic, Canceler } from 'axios';

interface IAction {
  type: string;
  payload?: object;
}

interface IState {
  response: any | null;
  isLoading: boolean;
  isError: boolean;
  error: any | null;
}

export const actions = {
  init: 'INIT',
  success: 'SUCCESS',
  fail: 'FAIL'
};

export const initialResponse: IState = {
  response: null,
  isLoading: false,
  isError: false,
  error: null
};

export function responseReducer(state: IState, action: IAction): IState {
  switch (action.type) {
    case actions.init:
      return { response: null, isLoading: true, isError: false, error: null };
    case actions.success:
      return { response: action.payload, isLoading: false, isError: false, error: null };
    case actions.fail:
      return { response: null, isLoading: false, isError: true, error: action.payload };
    default:
      return initialResponse;
  }
}

/**
 * Params
 * @param  {AxiosInstance} axios - (optional) The custom axios instance
 * @param  {string} initialUrl - The request URL
 * @param  {('GET'|'POST'|'PUT'|'DELETE'|'HEAD'|'OPTIONS'|'PATCH')} method - The request method
 * @param  {object} [options={}] - (optional) The config options of Axios.js (https://goo.gl/UPLqaK)
 * @param  {function} [callback=(error, response) => {}] - (optional) Custom handler callback, NOTE: `error` and `response` will be set to `null` before request
 */

export interface DataApiProperties {
  axios?: AxiosInstance | AxiosStatic;
  initialUrl?: string;
  method?: string;
  options?: any | null;
  callback?: Function;
}

/**
 * Returns
 * @param  {object} response - The response of Axios.js (https://goo.gl/dJ6QcV)
 * @param  {boolean} isLoading - The loading status
 * @param  {boolean} isError - The error status
 * @param  {object} error - HTTP error
 * @param  {object} setUrl - method to set the url
 */

interface DataApiReturn extends IState {
  setUrl: Function;
}

// Set up a cancellation token
const { CancelToken } = defaultAxios;

export const useDataApi = ({
  axios = defaultAxios,
  initialUrl = '', // empty means it will not be executed until setUrl is used
  method = 'get',
  options = null, // cannot be {} as this will trigger an endless loop in useEffect
  callback
}: DataApiProperties): DataApiReturn => {
  const [results, dispatch] = useReducer(responseReducer, initialResponse);
  const [url, setUrl] = useState<string>(initialUrl);

  const cancelRef = useRef<Canceler>();
  useEffect(() => {
    if (!url) return;

    let unmounted = false;

    const callbackHandler = (error: any, response: any) => {
      if (callback) {
        // note this call back mush be called with an useCallback to avoid endless loop
        callback(error, response);
      }
    };

    const fetchData = async () => {
      callbackHandler(null, null);
      dispatch({ type: actions.init });

      // cancel all previous requests
      if (cancelRef.current) {
        cancelRef.current('Cancelling all previous requests');
      }

      try {
        const response = await axios({
          url,
          method,
          ...options,
          cancelToken: new CancelToken(function executor(c) {
            cancelRef.current = c;
          })
        });

        if (!unmounted) {
          callbackHandler(null, response);
          dispatch({ type: actions.success, payload: response });
        }
      } catch (error: any) {
        // check that this error is not because we cancelled it ourselves				}
        if (!unmounted) {
          if (defaultAxios.isCancel(error)) {
            console.log(`Axios request cancelled: ${error.message}`);
          } else {
            callbackHandler(error, null);
            dispatch({ type: actions.fail, payload: error });
          }
        }
      } finally {
        unmounted = true;
      }
    };

    fetchData();
  }, [axios, callback, method, options, url]);

  return {
    ...results,
    setUrl
  };
};
