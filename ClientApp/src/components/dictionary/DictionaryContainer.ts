import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import { IStoreState } from '../../state/store';
import { IAuthState } from '../auth/types';
import DictionaryComponent from './DictionaryComponent';

export interface DictionaryRouterProps {
  word: string; // This one is coming from the router
  pattern: string; // This one is coming from the router
}

export interface DictionaryProps extends RouteComponentProps<DictionaryRouterProps> {
  authentication: IAuthState; // This one is coming from the redux store
}

const mapStateToProps = (state: IStoreState, ownProps: DictionaryProps): DictionaryProps => {
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

const DictionaryContainer = connect(mapStateToProps, mapDispatchToProps)(DictionaryComponent);

export default DictionaryContainer;
