import React from 'react';
import { Route, Redirect, RouteProps } from 'react-router-dom';
import { IStoreState } from '../../state/store';
import { store } from '../..';

interface PrivateRouteProps extends RouteProps {
  component: any;
}

function PrivateRoute(props: PrivateRouteProps) {
  const { component: Component, ...rest } = props;
  // const isSignedIn = !!localStorage.getItem('user');

  // get redux store
  const theStore: IStoreState = store.getState();

  let isSignedIn = false;
  if (theStore.authentication && theStore.authentication.logon && theStore.authentication.loggedIn) {
    isSignedIn = true;
  }

  return (
    <Route
      {...rest}
      render={(routeProps) =>
        isSignedIn ? (
          <Component {...routeProps} />
        ) : (
          <Redirect
            to={{
              pathname: '/login',
              state: { from: routeProps.location }
            }}
          />
        )
      }
    />
  );
}

export default PrivateRoute;
