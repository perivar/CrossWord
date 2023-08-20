import React, { useState, memo, useCallback, useEffect } from 'react';
import axios, { AxiosInstance, AxiosResponse } from 'axios';
import produce, { Draft } from 'immer';
import { useDataApi } from '../hooks/data-api-hook';

interface BulmaAutocompleteState {
  // The active selection's index
  activeSuggestion: number;
  // The suggestions that match the user's input
  filteredSuggestions: any[];
  // Whether or not the suggestion list is shown
  showSuggestions: boolean;
  // What the user has entered
  userInput: string;
}

const initialState: BulmaAutocompleteState = {
  // The active selection's index
  activeSuggestion: 0,
  // The suggestions that match the user's input
  filteredSuggestions: [],
  // Whether or not the suggestion list is shown
  showSuggestions: false,
  // What the user has entered
  userInput: ''
};

interface BulmaAutocompleteArguments {
  id: string;
  notFound: string;
  value?: string;
  onChangeValue?: (value: string) => void;
  // inputRef?: React.RefObject<HTMLInputElement>;
  placeholder?: string;
  suggestions?: any[];
  mandatory?: boolean; // whether a selection is mandatory
  baseUrl?: string;
  headers?: any;
  queryHandler?: (word: string) => string;
  responseHandler?: (response: any) => any[];
  requestInterceptor?: (request: any) => {};
  requestInterceptorErrorHandler?: (error: any) => {};
  responseInterceptor?: (response: any) => AxiosResponse<any> | Promise<AxiosResponse<any>>;
  responseInterceptorErrorHandler?: (error: any) => {};
}

// make a new axios instance, so that we don’t pollute the global axios object
const axiosInstance: AxiosInstance = axios.create({});

function Autocomplete(props: BulmaAutocompleteArguments) {
  const {
    id,
    notFound,
    // inputRef,
    value,
    onChangeValue,
    placeholder,
    suggestions = [],
    mandatory = true,
    baseUrl,
    headers,
    queryHandler,
    responseHandler,
    requestInterceptor,
    requestInterceptorErrorHandler,
    responseInterceptor,
    responseInterceptorErrorHandler
  } = props;

  const [state, setState] = useState<BulmaAutocompleteState>(initialState);

  if (baseUrl) axiosInstance.defaults.baseURL = baseUrl;
  if (headers) axiosInstance.defaults.headers = headers;

  // add interceptors if they were passed
  if (requestInterceptor && requestInterceptorErrorHandler) {
    axiosInstance.interceptors.request.use(requestInterceptor, requestInterceptorErrorHandler);
  }
  if (responseInterceptor && responseInterceptorErrorHandler) {
    axiosInstance.interceptors.response.use(responseInterceptor, responseInterceptorErrorHandler);
  }

  const {
    response,
    error,
    isLoading,
    setUrl: fetchData
  } = useDataApi({
    // isError
    axios: axiosInstance
  });

  // Event fired when the user presses a key down
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      const { activeSuggestion, filteredSuggestions } = state;

      // User pressed the enter key, update the input and close the
      // suggestions
      if (e.keyCode === 13 || e.keyCode === 9) {
        e.preventDefault();

        if (mandatory && filteredSuggestions.length === 0) return;

        setState(
          produce((draft: Draft<BulmaAutocompleteState>) => {
            draft.activeSuggestion = 0;
            draft.showSuggestions = false;
            draft.userInput = filteredSuggestions[activeSuggestion];
            if (onChangeValue) onChangeValue(filteredSuggestions[activeSuggestion]);
          })
        );
      }
      // User pressed the up arrow, decrement the index
      else if (e.keyCode === 38) {
        e.preventDefault();

        if (activeSuggestion === 0) {
          return;
        }

        setState(
          produce((draft: Draft<BulmaAutocompleteState>) => {
            draft.activeSuggestion -= 1;
          })
        );
      }
      // User pressed the down arrow, increment the index
      else if (e.keyCode === 40) {
        e.preventDefault();

        if (activeSuggestion + 1 === filteredSuggestions.length) {
          return;
        }

        setState(
          produce((draft: Draft<BulmaAutocompleteState>) => {
            draft.activeSuggestion += 1;
          })
        );
      }
    },
    [mandatory, onChangeValue, state]
  );

  // Event fired when the input value is changed
  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      e.preventDefault();
      const userInput = e.currentTarget.value;

      let filteredSuggestions: any[] = [];
      if (baseUrl) {
        // use server side fetching
        let url = '';
        if (queryHandler) {
          url = queryHandler(userInput);
        } else {
          url = userInput;
        }
        fetchData(url);
      } else {
        // Filter our suggestions that don't contain the user's input
        filteredSuggestions = suggestions.filter(
          (suggestion) => suggestion.toLowerCase().indexOf(userInput.toLowerCase()) > -1
        );
      }

      // Update the user input and filtered suggestions, reset the active
      // suggestion and make sure the suggestions are shown
      setState(
        produce((draft: Draft<BulmaAutocompleteState>) => {
          draft.activeSuggestion = 0;
          draft.filteredSuggestions = filteredSuggestions;
          draft.showSuggestions = true;
          draft.userInput = userInput;
        })
      );
    },
    [baseUrl, fetchData, queryHandler, suggestions]
  );

  // Event fired when the user clicks on a suggestion
  const handleClick = useCallback(
    (e: React.MouseEvent<HTMLDivElement, MouseEvent>) => {
      e.preventDefault();
      const currentValue = e.currentTarget.innerText;
      // Update the user input and reset the rest of the state
      setState(
        produce((draft: Draft<BulmaAutocompleteState>) => {
          draft.activeSuggestion = 0;
          draft.filteredSuggestions = [];
          draft.showSuggestions = false;
          draft.userInput = currentValue;
          if (onChangeValue) onChangeValue(currentValue);
        })
      );
    },
    [onChangeValue]
  );

  const handleClear = useCallback(
    (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {
      e.preventDefault();
      // if (inputRef && inputRef.current) inputRef.current.value = '';

      // Update the user input and reset the rest of the state
      setState(
        produce((draft: Draft<BulmaAutocompleteState>) => {
          draft.activeSuggestion = 0;
          draft.filteredSuggestions = [];
          draft.showSuggestions = false;
          draft.userInput = '';
          if (onChangeValue) onChangeValue('');
        })
      );
    },
    [onChangeValue]
  );

  const handleNonFoundClick = useCallback(
    (e: React.MouseEvent<HTMLDivElement, MouseEvent>) => {
      e.preventDefault();

      if (mandatory) return;

      setState(
        produce((draft: Draft<BulmaAutocompleteState>) => {
          draft.activeSuggestion = 0;
          draft.filteredSuggestions = [];
          draft.showSuggestions = false;
        })
      );
    },
    [mandatory]
  );

  useEffect(() => {
    if (response) {
      // console.log('useEffect() - handling load success (response)');

      let filteredSuggestions: any[] = [];
      if (responseHandler) {
        filteredSuggestions = responseHandler(response);
      } else {
        filteredSuggestions = response.data;
      }

      // Update the user input and filtered suggestions, reset the active
      // suggestion and make sure the suggestions are shown
      setState(
        produce((draft: Draft<BulmaAutocompleteState>) => {
          draft.activeSuggestion = 0;
          draft.filteredSuggestions = filteredSuggestions;
          draft.showSuggestions = true;
        })
      );
    }
  }, [response]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (error) {
      // console.log('useEffect() - handling load error');
      // console.error(error);
    }
  }, [error]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    // console.log('useEffect() - autocomplete value has changed: "' + value + '"');

    setState(
      produce((draft: Draft<BulmaAutocompleteState>) => {
        draft.userInput = value || '';
      })
    );
  }, [value]); // eslint-disable-line react-hooks/exhaustive-deps

  // suggestions list component
  const suggestionsListComponent = useCallback(() => {
    const { activeSuggestion, filteredSuggestions, showSuggestions, userInput } = state;

    if (showSuggestions && userInput) {
      if (filteredSuggestions.length) {
        return (
          <div className="dropdown-content">
            {filteredSuggestions.map((suggestion, index) => {
              return (
                <div
                  key={index}
                  role="presentation"
                  className={`dropdown-item${index === activeSuggestion ? ' is-active' : ''}`}
                  onClick={handleClick}>
                  {typeof suggestion === 'string' ? suggestion : ''}
                </div>
              );
            })}
          </div>
        );
      }
      return (
        <div className="dropdown-content">
          <div className="dropdown-item" role="presentation" onClick={handleNonFoundClick}>
            {notFound}
          </div>
        </div>
      );
    }

    return <></>;
  }, [handleClick, handleNonFoundClick, notFound, state]);

  return (
    <div className="dropdown is-active">
      <div className="dropdown-trigger control has-icons-right">
        <input
          id={id}
          // ref={inputRef}
          className="input"
          type="text"
          onChange={handleChange}
          onKeyDown={handleKeyDown}
          value={state.userInput}
          placeholder={placeholder}
          aria-haspopup="true"
          aria-controls="dropdown-menu"
          autoComplete="off"
          spellCheck={false}
          autoCorrect="off"
        />
        <button type="button" className="icon is-small is-right is-icon-button" onClick={handleClear}>
          <i className="fas fa-times fa-xs" />
        </button>
        {isLoading && <div className="is-loading" />}
      </div>
      <div className="dropdown-menu" id="dropdown-menu" role="menu">
        {suggestionsListComponent()}
      </div>
    </div>
  );
}

export const BulmaAutocomplete = memo(Autocomplete);
// export const BulmaAutocomplete = Autocomplete;
