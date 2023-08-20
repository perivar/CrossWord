import { userActions } from './ducks/actions';
import { UserActionTypes } from './ducks/types';
import { IStoreState } from '../../state/store';

let buffer: any[] = [];

// eslint-disable-next-line consistent-return
export const jwt = (store: any) => (next: any) => (action: any) => {
  buffer.push(action);
  if (action.type === 'INVALID_TOKEN') {
    const theStore: IStoreState = store.getState();
    if (
      theStore.authentication &&
      theStore.authentication.logon &&
      theStore.authentication.logon.token &&
      theStore.authentication.logon.refreshToken
    ) {
      if (!theStore.authentication.pendingRefreshingToken) {
        store.dispatch({ type: UserActionTypes.REFRESHING_TOKEN });
        store
          .dispatch(
            userActions.refreshToken(theStore.authentication.logon.token, theStore.authentication.logon.refreshToken)
          )
          .then(() => {
            // this will fire even if the refresh token is still valid or not.
            // if the refresh token is not valid (and therefore not able to retrieve
            // a new auth token), the REFRESH_EXPIRED action is fired from operations.ts.
            store.dispatch({ type: UserActionTypes.TOKEN_REFRESHED });

            // get the action before the last INVALID_TOKEN (the one which got denied because of token expiration)
            const pos = buffer.map((e) => e.type).indexOf('INVALID_TOKEN') - 1;

            // count back from the invalid token dispatch, and fire off the last dispatch again which was
            // a function. These are to be dispatched, and have the dispatch function passed through to them.
            for (let i = pos; i > -1; i -= 1) {
              if (typeof buffer[i] === 'function') {
                store.dispatch({
                  type: 'RESEND',
                  action: buffer[i](store.dispatch)
                });
                break;
              }
            }
            buffer = [];
          });
      }
    }
  } else if (action.type === 'REFRESH_EXPIRED') {
    buffer = [];
    store.dispatch(userActions.logout());
  } else {
    if (buffer.length > 20) {
      // remove all items but keep the last 20 which forms the buffer
      buffer.splice(0, buffer.length - 20);
    }
    return next(action);
  }
};
