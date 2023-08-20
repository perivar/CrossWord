/* eslint-disable import/no-extraneous-dependencies */
/* eslint-disable @typescript-eslint/no-var-requires */
import React, { Component } from 'react';
import { Route, Router } from 'react-router-dom';
import { connect } from 'react-redux';

import { Counter } from './components/counter/Counter';
import ForecastContainer from './components/forecast/ForecastContainer';
// import { Home } from "./components/Home";
import { Layout } from './components/shared/Layout';

import LoginContainer from './components/auth/LoginContainer';
import RegisterContainer from './components/auth/RegisterContainer';
import ForgottenPasswordContainer from './components/auth/ForgottenPasswordContainer';
import HelpContainer from './components/auth/HelpContainer';

import HomeContainer from './components/auth/HomeContainer';
import PrivateRoute from './components/auth/PrivateRouteComponent';
import CrosswordContainer from './components/crossword/CrosswordContainer';
import DictionaryContainer from './components/dictionary/DictionaryContainer';
import * as alertActions from './components/alert/ducks/actions';

import { history } from './history';

import './components/shared/bulma.scss';
import { IStoreState } from './state/store';
import StatusContainer from './components/status/StatusContainer';

// Add a tool that will notify us when components update
// if (process.env.NODE_ENV !== 'production') {
//   console.warn('Loading why-did-you-render');
//   const whyDidYouRender = require('@welldone-software/why-did-you-render');
//   whyDidYouRender(React);
// }

function TestPage({ match, location }: { match: any; location: any }) {
  return (
    <>
      <p>
        <strong>Match Props: </strong>
        {JSON.stringify(match, null, 2)}
      </p>
      <p>
        <strong>Location Props: </strong>
        {JSON.stringify(location, null, 2)}
      </p>
    </>
  );
}

class App extends Component<any, any> {
  constructor(props: any) {
    super(props);

    history.listen(() => {
      // history arguments - unused: location, action
      // clear alert on location change
      this.props.clearAlerts();
    });
  }

  public render() {
    const { alert } = this.props;
    return (
      <>
        {alert && alert.message && <div className="notification is-warning">{alert.message}</div>}
        <Router history={history}>
          <Layout>
            {/* <Route exact={true} path="/" component={Home} /> */}
            <PrivateRoute exact path="/" component={HomeContainer} />
            <Route path="/dictionary" component={DictionaryContainer} />
            <Route exact path="/counter" component={Counter} />
            <Route exact path="/status" component={StatusContainer} />
            <Route exact path="/forecast" component={ForecastContainer} />
            <Route exact path="/login" component={LoginContainer} />
            <Route exact path="/register" component={RegisterContainer} />

            {/* To define an optional parameter, you do:
						   <Route path="/to/page/:pathParam?" component={MyPage} />
						 and for multiple optional parameters:
						   <Route path="/to/page/:pathParam1?/:pathParam2?" component={MyPage} /> */}
            <Route exact path="/forgotten-password/:username?/:token?" component={ForgottenPasswordContainer} />
            <Route exact path="/help" component={HelpContainer} />
            <Route exact path="/crossword" component={CrosswordContainer} />
            <Route exact path="/test" component={TestPage} />
            <Route exact path="/test/:id" component={TestPage} />
          </Layout>
        </Router>
      </>
    );
  }
}

function mapStateToProps(state: IStoreState) {
  const { alert } = state;
  const { authentication } = state;
  return {
    alert,
    authentication
  };
}

// inject methods *and* dispatch
const mapDispatchToProps = (dispatch: any) => {
  return {
    // dispatch,
    clearAlerts: () => dispatch(alertActions.clear())
  };
};

const connectedApp = connect(mapStateToProps, mapDispatchToProps)(App);

export default connectedApp;
