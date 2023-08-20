import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import ForgottenPasswordComponent from './ForgottenPasswordComponent';
import { IStoreState } from '../../state/store';
import { IAuthState } from './types';

export interface ForgottenPasswordRouterProps {
  token: string; // This one is coming from the router
  username: string; // This one is coming from the router
}

export interface ForgottenPasswordProps extends RouteComponentProps<ForgottenPasswordRouterProps> {
  authentication: IAuthState; // This one is coming from the redux store
}

const mapStateToProps = (state: IStoreState, ownProps: ForgottenPasswordProps): ForgottenPasswordProps => {
  const { authentication } = state;
  return {
    ...ownProps, // to make routes work, map state to props (add the properties after the spread)
    authentication
  };
};

// inject methods *and* dispatch
const mapDispatchToProps = () => {
  return {
    // dispatch
  };
};

const ForgottenPasswordContainer = connect(mapStateToProps, mapDispatchToProps)(ForgottenPasswordComponent);

export default ForgottenPasswordContainer;
