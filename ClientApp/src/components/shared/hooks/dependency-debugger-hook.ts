import _ from 'lodash';
import { useRef, useMemo } from 'react';

// https://stackoverflow.com/questions/55187563/determine-which-dependency-array-variable-caused-useeffect-hook-to-fire
// This can then be used by copying a dependency array literal and just changing it to be an object literal:
//
// useDependenciesDebugger({ state1, state2 });

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
    }
  });
}

const isEmptyObj = (object: any) =>
  !Object.getOwnPropertySymbols(object).length && !Object.getOwnPropertyNames(object).length;

const compareInputs = (inputKeys: any, oldInputs: any, newInputs: any) => {
  inputKeys.forEach((key: any) => {
    const oldInput = oldInputs[key];
    const newInput = newInputs[key];
    if (oldInput !== newInput) {
      console.log('change detected', key, 'old:', oldInput, 'new:', newInput);
      // console.log(JSON.stringify(difference(oldInput, newInput)));
      const diff = difference(oldInput, newInput);
      if (!isEmptyObj(diff)) console.log(diff);
    }
  });
};

export const useDependenciesDebugger = (inputs: any): any => {
  const oldInputsRef = useRef(inputs);
  const inputValuesArray = Object.values(inputs);
  const inputKeysArray = Object.keys(inputs);

  useMemo(() => {
    const oldInputs = oldInputsRef.current;

    compareInputs(inputKeysArray, oldInputs, inputs);

    oldInputsRef.current = inputs;
  }, inputValuesArray); // eslint-disable-line react-hooks/exhaustive-deps
};
