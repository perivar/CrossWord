import { connect } from 'react-redux';
import { ThunkDispatch } from 'redux-thunk';
import { AnyAction } from 'redux';
import RegisterComponent from './RegisterComponent';

import { userActions } from './ducks/actions';
import { IUser } from './types';
import { IStoreState } from '../../state/store';

const mapStateToProps = (state: IStoreState, ownProps: any) => {
  const { registering } = state.registration;
  return {
    ...ownProps, // to make routes work, map state to props (add the properties after the spread)
    registering
  };
};

// inject methods *and* dispatch
const mapDispatchToProps = (dispatch: ThunkDispatch<IStoreState, any, AnyAction>) => {
  return {
    dispatch,
    register: (user: IUser) => dispatch(userActions.register(user))
  };
};

const RegisterContainer = connect(mapStateToProps, mapDispatchToProps)(RegisterComponent);

export default RegisterContainer;
