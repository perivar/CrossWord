import { connect } from 'react-redux';
import { ThunkDispatch } from 'redux-thunk';
import { AnyAction } from 'redux';
import StatusComponent from './StatusComponent';

import { IStoreState } from '../../state/store';

const mapStateToProps = (state: IStoreState, ownProps: any) => {
  const { authentication } = state;
  const { logon } = authentication;
  return {
    ...ownProps, // to make routes work, map state to props (add the properties after the spread)
    logon
  };
};

// inject methods *and* dispatch
const mapDispatchToProps = (dispatch: ThunkDispatch<IStoreState, any, AnyAction>) => {
  return {
    dispatch
  };
};

const StatusContainer = connect(mapStateToProps, mapDispatchToProps)(StatusComponent);

export default StatusContainer;
