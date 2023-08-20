import _ from 'lodash';
import { useEffect, useRef } from 'react';

// version a: https://usehooks.com/useWhyDidYouUpdate/
// version b: (this): https://github.com/devhubapp/devhub/blob/master/packages/components/src/hooks/use-why-did-you-update.ts

/**
 * Deep diff between two object, using lodash
 * @param  {Object} newObject Object compared
 * @param  {Object} baseObject   Object to compare with
 * @return {Object} Return a new object who represent the diff
 */
function difference(newObject: any, baseObject: any) {
  return _.transform(newObject, (result: any, value, key) => {
    if (!_.isEqual(value, baseObject[key])) {
      result[key] = _.isObject(value) && _.isObject(baseObject[key]) ? difference(value, baseObject[key]) : value;
      // console.log('Result: ' + JSON.stringify(result));
    }
  });
}

export function useWhyDidYouUpdate(name: string, props: Record<string, any>): any {
  // Get a mutable ref object where we can store props ...
  // ... for comparison next time this hook runs.
  const latestProps = useRef(props);

  useEffect(() => {
    if (process.env.NODE_ENV === 'production') {
      return;
    }

    if (latestProps.current) {
      // Get all keys from previous and current props
      const allKeys = Object.keys({ ...latestProps.current, ...props });

      // Use this object to keep track of changed props
      // const changesObj: Record<string, { from: any; to: any }> = {};
      const changesObj: Record<string, { from: any; to: any; isDeepEqual: boolean; difference: any }> = {};

      // Iterate through keys
      allKeys.forEach((key) => {
        // If previous is different from current
        if (latestProps.current[key] !== props[key]) {
          // Add to changesObj
          changesObj[key] = {
            from: latestProps.current[key],
            to: props[key],
            isDeepEqual: _.isEqual(props[key], latestProps.current[key]),
            difference: difference(props[key], latestProps.current[key])
          };
        }
      });

      // If changesObj not empty then output to console
      if (Object.keys(changesObj).length) {
        console.log('[why-did-you-update]', name, changesObj);
      }
    }

    // Finally update previousProps with current props for next hook call
    latestProps.current = props;
  }); // , [name, props]);
}
