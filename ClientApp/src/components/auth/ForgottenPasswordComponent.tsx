import React, { useCallback, useEffect, FormEvent, ChangeEvent, useReducer } from 'react';
import { Link } from 'react-router-dom';
import { useDataApi } from '../shared/hooks/data-api-hook';
import { BulmaInputField } from '../shared/bulma-components/BulmaInputField';
import { BulmaSubmitButton } from '../shared/bulma-components/BulmaSubmitButton';
import { ForgottenPasswordProps } from './ForgottenPasswordContainer';
import { history } from '../../history';
import { ASPCoreIdentityErrors } from './types';
// import { useReducerWithLogger } from '../shared/hooks/reducer-logger-hook';

export enum ActionTypes {
  USERNAME_CHANGE = 'USERNAME_CHANGE',
  PASSWORD_CHANGE = 'PASSWORD_CHANGE',
  CONFIRM_PASSWORD_CHANGE = 'CONFIRM_PASSWORD_CHANGE',
  SUBMIT = 'SUBMIT',
  RECEIVED_TOKEN = 'RECEIVED_TOKEN',
  RECEIVED_RESET_SUCCESS = 'RECEIVED_RESET_SUCCESS',
  QUERY_PARAMETERS_CHANGE = 'QUERY_PARAMETERS_CHANGE'
}

interface IUsernameChange {
  type: typeof ActionTypes.USERNAME_CHANGE;
  value: string;
}

interface IPasswordChange {
  type: typeof ActionTypes.PASSWORD_CHANGE;
  value: string;
}

interface IConfirmPasswordChange {
  type: typeof ActionTypes.CONFIRM_PASSWORD_CHANGE;
  value: string;
}

interface ISubmit {
  type: typeof ActionTypes.SUBMIT;
  username: string;
  password: string;
  confirmPassword: string;
}

interface IReceivedToken {
  type: typeof ActionTypes.RECEIVED_TOKEN;
  token: string;
}

interface IReceivedResetSuccess {
  type: typeof ActionTypes.RECEIVED_RESET_SUCCESS;
  value: string;
}

interface IQueryParametersChange {
  type: typeof ActionTypes.QUERY_PARAMETERS_CHANGE;
  username: string;
  token: string;
}

type Actions =
  | IUsernameChange
  | IPasswordChange
  | IConfirmPasswordChange
  | ISubmit
  | IReceivedToken
  | IReceivedResetSuccess
  | IQueryParametersChange;

interface IState {
  username: string;
  password: string;
  confirmPassword: string;
  submitted: boolean;
  passwordsNotEqual: boolean;
  passwordNotValid: boolean;
  passwordUpdatedSuccessfully: boolean;
  hasTokenParameter: boolean;
  hasUsernameParameter: boolean;
  apiUrl: string | undefined;
  token: string;
  getResetToken: Function;
  doResetPassword: Function;
}

export function stateReducer(state: IState, action: Actions): IState {
  switch (action.type) {
    case ActionTypes.USERNAME_CHANGE:
      return {
        ...state,
        username: action.value
      };
    case ActionTypes.PASSWORD_CHANGE:
      return {
        ...state,
        password: action.value
      };
    case ActionTypes.CONFIRM_PASSWORD_CHANGE:
      return {
        ...state,
        confirmPassword: action.value
      };
    case ActionTypes.QUERY_PARAMETERS_CHANGE: {
      const hasTokenParameter = !!action.token;
      const hasUsernameParameter = !!action.username;

      let { username } = state;
      if (hasUsernameParameter) username = decodeURIComponent(action.username);

      let { token } = state;
      if (hasTokenParameter) token = decodeURIComponent(action.token);

      return {
        ...state,
        username,
        token,
        hasUsernameParameter,
        hasTokenParameter
      };
    }
    case ActionTypes.RECEIVED_TOKEN:
      return {
        ...state,
        token: action.token,
        // if the token is passed as an url parameter, we want a "clean" password form
        submitted: false
      };
    case ActionTypes.RECEIVED_RESET_SUCCESS:
      return {
        ...state,
        passwordUpdatedSuccessfully: true,
        submitted: false
      };
    case ActionTypes.SUBMIT:
      if (!state.hasTokenParameter && state.username) {
        state.getResetToken(`${state.apiUrl}/api/Account/GenerateForgotPasswordToken?username=${state.username}`);
      } else if (
        state.hasTokenParameter &&
        state.hasUsernameParameter &&
        state.password &&
        !isValidASPCoreIdentityPassword(state.password)
      ) {
        return {
          ...state,
          passwordNotValid: true,
          password: '',
          confirmPassword: '',
          submitted: true
        };
      } else if (
        state.hasTokenParameter &&
        state.hasUsernameParameter &&
        state.password &&
        isValidASPCoreIdentityPassword(state.password) &&
        state.password === state.confirmPassword
      ) {
        if (state.token) {
          const encodedToken = encodeURIComponent(state.token);
          const encodedUsername = encodeURIComponent(state.username);
          const encodedPassword = encodeURIComponent(state.password);

          state.doResetPassword(
            `${state.apiUrl}/api/Account/ResetPassword?username=${encodedUsername}&password=${encodedPassword}&token=${encodedToken}`
          );
        }
      } else if (state.password && state.password !== state.confirmPassword) {
        return {
          ...state,
          passwordsNotEqual: true,
          confirmPassword: '',
          submitted: true
        };
      } else {
        // all fields are empty - set to submitted so the warnings are shown
        return {
          ...state,
          submitted: true
        };
      }
      return state;

    default:
      return state;
  }
}

const isValidASPCoreIdentityPassword = (password: string): boolean => {
  // https://stackoverflow.com/questions/48635152/regex-for-default-asp-net-core-identity-password
  if (/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#$^+=!*()@%&]).{8,}$/.test(password)) {
    return true;
  }
  return false;
  // return true;
};

export default function ForgottenPasswordComponent(props: ForgottenPasswordProps): JSX.Element {
  // get apiUrl from config object
  const config = { apiUrl: process.env.REACT_APP_API };

  // get login user name from the redux store
  const { authentication } = props;
  const initialUsername = authentication.logonUserName;

  // setup the data api hooks
  // data api for receiving reset password tokens
  const {
    response: responseToken,
    isLoading: isLoadingToken,
    isError: isErrorToken,
    error: errorToken,
    setUrl: getResetToken
  } = useDataApi({
    initialUrl: ''
    // callback: receivedToken
  });

  // data api for resetting the password
  const {
    response: responseReset,
    // isLoading: isLoadingReset,
    isError: isErrorReset,
    error: errorReset,
    setUrl: doResetPassword
  } = useDataApi({
    initialUrl: ''
    // callback: receivedResetConfirmation
  });

  // setup state with some initial default parameters
  const initialState: IState = {
    username: initialUsername,
    apiUrl: config.apiUrl,
    getResetToken,
    doResetPassword,
    // initial empty values
    password: '',
    confirmPassword: '',
    submitted: false,
    passwordsNotEqual: false,
    passwordNotValid: false,
    passwordUpdatedSuccessfully: false,
    hasTokenParameter: false,
    hasUsernameParameter: false,
    token: ''
  };

  // use the useReducer instead of useState due to the complexity of the state handling
  const [state, dispatch] = useReducer(stateReducer, initialState);
  // const [state, dispatch] = useReducerWithLogger(stateReducer, initialState);

  // set query parameters from the match props if  they change
  useEffect(() => {
    const { username } = props.match.params;
    const { token } = props.match.params;
    if (username && token) {
      dispatch({
        type: ActionTypes.QUERY_PARAMETERS_CHANGE,
        username,
        token
      });
    }
  }, [dispatch, props.match.params, props.match.params.token, props.match.params.username]);

  // callback for receiving a token from an api
  // note these callbacks mush be called with an useCallback to avoid endless loop
  const receivedToken = useCallback(
    (error: any, response: any) => {
      if (response && response.data) {
        const token = response.data;
        dispatch({ type: ActionTypes.RECEIVED_TOKEN, token });

        const encodedToken = encodeURIComponent(state.token);
        const encodedUsername = encodeURIComponent(state.username);

        // instead of sending the token on email, we include it as a url parameter
        history.push(`/forgotten-password/${encodedUsername}/${encodedToken}`);

        console.log(`encoded username: ${encodedUsername}`);
        console.log(`encoded token: ${encodedToken}`);
      }
    },
    [dispatch, state.token, state.username]
  );

  // callback for resetting the password using an api
  // note these callbacks mush be called with an useCallback to avoid endless loop
  const receivedResetConfirmation = useCallback(
    (error: any, response: any) => {
      if (response) {
        dispatch({ type: ActionTypes.RECEIVED_RESET_SUCCESS, value: response });
      }
    },
    [dispatch]
  );

  // instead of using the callback in the data api hook we can use the useEffect hook to monitor the response
  useEffect(() => {
    receivedToken(errorToken, responseToken);
  }, [responseToken, errorToken, receivedToken]);

  useEffect(() => {
    receivedResetConfirmation(errorReset, responseReset);
  }, [responseReset, errorReset, receivedResetConfirmation]);

  // change event handlers
  const handleUsernameChange = (e: ChangeEvent<HTMLInputElement>) => {
    dispatch({
      type: ActionTypes.USERNAME_CHANGE,
      value: e.currentTarget.value
    });
  };

  const handlePasswordChange = (e: ChangeEvent<HTMLInputElement>) => {
    dispatch({
      type: ActionTypes.PASSWORD_CHANGE,
      value: e.currentTarget.value
    });
  };

  const handleConfirmPasswordChange = (e: ChangeEvent<HTMLInputElement>) => {
    dispatch({
      type: ActionTypes.CONFIRM_PASSWORD_CHANGE,
      value: e.currentTarget.value
    });
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    dispatch({
      type: ActionTypes.SUBMIT,
      username: state.username,
      password: state.password,
      confirmPassword: state.confirmPassword
    });
  };

  const usernameSegment = (
    <BulmaInputField
      label="Brukernavn"
      type="text"
      name="username"
      placeholder="feks. ola@nordmann.no"
      required
      requiredMessage="Gyldig brukernavn (e-post adresse) er påkrevd"
      value={state.username}
      submitted={state.submitted}
      handleChange={handleUsernameChange}
      icon={<i className="fas fa-user" />}
    />
  );

  const passwordSegment = (
    <>
      <div className="is-divider" data-content="Passord" />

      <BulmaInputField
        label="Nytt Passord"
        type="password"
        name="password"
        placeholder="*********"
        required
        requiredMessage={state.passwordNotValid ? 'Passordet er ikke gyldig' : 'Gyldig passord er påkrevd'}
        value={state.password}
        submitted={state.submitted}
        handleChange={handlePasswordChange}
        icon={<i className="fa fa-lock" />}
      />

      <BulmaInputField
        label="Bekreft Passord"
        type="password"
        name="confirmPassword"
        placeholder="*********"
        required
        requiredMessage={state.passwordsNotEqual ? 'Passordene er ulike' : 'Gyldig passord er påkrevd'}
        value={state.confirmPassword}
        submitted={state.submitted}
        handleChange={handleConfirmPasswordChange}
        icon={<i className="fa fa-lock" />}
      />
    </>
  );

  const formSegment = (
    <form noValidate className="box" onSubmit={handleSubmit}>
      <div className="field has-text-centered">
        <i className="fa fa-lock fa-3x" />
      </div>

      {usernameSegment}
      {state.hasTokenParameter ? passwordSegment : <></>}

      <BulmaSubmitButton text="Oppdater passord" loading={isLoadingToken} />
    </form>
  );

  return (
    <>
      {/* <pre className="has-text-left">{JSON.stringify(matchProps, null, 2)}</pre> */}

      {isErrorToken && (
        <article className="message is-danger">
          <div className="message-body">
            {errorToken && errorToken.response && errorToken.response.data ? (
              errorToken.response.data.title
            ) : (
              <pre className="has-text-left">{JSON.stringify(errorToken.response.data || {}, null, 0)}</pre>
            )}
          </div>
        </article>
      )}

      {/* isErrorReset && (
        <div>
          Noe gikk galt!
          <pre className="has-text-left">{JSON.stringify(errorMessageReset || {}, null, 0)}</pre>
        </div>
			)} */}

      {isErrorReset ? (
        <article className="message is-danger">
          <div className="message-body">
            {errorReset &&
              errorReset.response &&
              errorReset.response.data &&
              errorReset.response.data.map((identityError: ASPCoreIdentityErrors) => (
                <div key={identityError.code} className="has-text-left">
                  {identityError.description}
                </div>
              ))}
          </div>
        </article>
      ) : (
        ''
      )}

      {/* {isLoadingToken ? (
        <div>Loading ...</div>
      ) : (
        <pre className="has-text-left">{JSON.stringify(responseToken || {}, null, 0)}</pre>
      )}
      {isLoadingReset ? (
        <div>Loading ...</div>
      ) : (
        <pre className="has-text-left">{JSON.stringify(responseReset || {}, null, 0)}</pre>
      )} */}

      <p className="subtitle has-text-grey has-text-centered">Glemt passord</p>
      <div className="container">
        <div className="columns is-centered">
          <div className="column is-5-tablet is-5-desktop is-4-widescreen">
            {state.passwordUpdatedSuccessfully ? (
              <h3 className="title is-3 has-text-success">Passordet ditt er oppdatert!</h3>
            ) : (
              formSegment
            )}
            <p className="has-text-grey">
              <Link to="/login">
                <i className="fas fa-user" />
                &nbsp; Har du allerede bruker? Logg inn!
              </Link>
            </p>
            <p className="has-text-grey">
              <Link to="/register">
                <i className="fas fa-user-plus" />
                &nbsp; Er du ikke bruker? Register deg nå!
              </Link>
            </p>
            <p className="has-text-grey">
              <Link to="/help">
                <i className="fas fa-question" />
                &nbsp; Trenger du hjelp?
              </Link>
            </p>
          </div>
        </div>
      </div>
    </>
  );
}
