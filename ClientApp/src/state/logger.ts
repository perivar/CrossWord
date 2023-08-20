// https://redux.js.org/advanced/middleware#the-final-approach

export const logger = (store: any) => (next: any) => (action: any) => {
  console.group(action.type);
  console.log('dispatching', action);
  const result = next(action);
  console.log('next state', store.getState());
  console.groupEnd();
  return result;
};
