import { connect } from 'react-redux';
import { ThunkDispatch } from 'redux-thunk';
import { AnyAction } from 'redux';
import LoginComponent from './LoginComponent';

import { userActions } from './ducks/actions';
import { IStoreState } from '../../state/store';

const mapStateToProps = (state: IStoreState, ownProps: any) => {
  const { loggingIn } = state.authentication;
  return {
    ...ownProps, // to make routes work, map state to props (add the properties after the spread)
    loggingIn
  };
};

// inject methods *and* dispatch
const mapDispatchToProps = (dispatch: ThunkDispatch<IStoreState, any, AnyAction>) => {
  return {
    dispatch,
    login: (username: string, password: string) => dispatch(userActions.login(username, password)),
    logout: () => dispatch(userActions.logout())
  };
};

const LoginContainer = connect(mapStateToProps, mapDispatchToProps)(LoginComponent);

export default LoginContainer;
