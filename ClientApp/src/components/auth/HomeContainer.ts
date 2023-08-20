import { connect } from 'react-redux';
import { ThunkDispatch } from 'redux-thunk';
import { AnyAction } from 'redux';
import HomeComponent from './HomeComponent';

import { userActions } from './ducks/actions';
import { IStoreState } from '../../state/store';

const mapStateToProps = (state: IStoreState, ownProps: any) => {
  const { users, authentication } = state;
  const { logon } = authentication;
  return {
    ...ownProps, // to make routes work, map state to props (add the properties after the spread)
    logon,
    users
  };
};

// inject methods *and* dispatch
const mapDispatchToProps = (dispatch: ThunkDispatch<IStoreState, any, AnyAction>) => {
  return {
    dispatch,
    getAll: () => dispatch(userActions.getAll()),
    delete: (username: string) => dispatch(userActions.delete(username))
  };
};

const HomeContainer = connect(mapStateToProps, mapDispatchToProps)(HomeComponent);

export default HomeContainer;
