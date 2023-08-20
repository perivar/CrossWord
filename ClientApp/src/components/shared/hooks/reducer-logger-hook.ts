import { useEffect, useReducer, useRef, useMemo } from 'react';

// https://staleclosures.dev/building-usereducer-with-logger/

// We are going to borrow a concept from functional programming called higher order functions and replace our dispatch function with a custom one.
function withLogger(dispatch: any): any {
  return (action: any) => {
    console.groupCollapsed('Action Type:', action.type);
    return dispatch(action);
  };
}

export const useReducerWithLogger = (reducer: any, initialState: any, initializer?: undefined): any => {
  // What about previous state?
  // If we open handy React documentation we will find the answer to our question: useRef.
  // The object it returns won't be recreated after every render. It can be used similarly to class instance properties.
  const prevState = useRef(initialState);
  const [state, dispatch] = useReducer(reducer, initialState, initializer);

  // If we don't want the function to be recreated on every render we can also memoize it.
  const dispatchWithLogger = useMemo(() => {
    return withLogger(dispatch);
  }, [dispatch]);

  useEffect(() => {
    if (state !== initialState) {
      console.log('Prev state: ', prevState.current);
      console.log('Next state: ', state);
      console.groupEnd();
    }
    prevState.current = state;
  }, [initialState, state]);

  return [state, dispatchWithLogger];
};
