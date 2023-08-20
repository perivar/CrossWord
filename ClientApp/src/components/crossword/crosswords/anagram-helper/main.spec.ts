/* eslint-disable @typescript-eslint/no-empty-function */
import sortBy from 'lodash/sortBy';
import { AnagramHelper } from './main';

// used by AnagramHelper
jest.mock('../../svgs/close.svg', () => 'Close');

jest.mock('react', () => ({
  Component: function Component() {},
  PureComponent: function PureComponent() {}
}));

jest.mock('react-dom', () => ({
  findDOMNode: () => ({
    focus: () => undefined
  })
}));

interface IExpectedResult {
  value: string;
  entered: boolean;
}

interface ITestCase {
  entries: string[];
  word: string;
  expected: IExpectedResult[];
}

const TestCases: ITestCase[] = [
  {
    entries: ['', '', '', '', 'l', '', 'e'],
    word: 'liberal',
    expected: [
      { value: 'l', entered: true },
      { value: 'i', entered: false },
      { value: 'b', entered: false },
      { value: 'e', entered: true },
      { value: 'r', entered: false },
      { value: 'a', entered: false },
      { value: 'l', entered: false }
    ]
  },

  // when there's an incorrect entry we should still highlight the correct ones
  {
    entries: ['s', '', '', '', 'h', '', '', '', '', '', '', ''],
    word: 'atennistutor',
    expected: [
      { value: 'a', entered: false },
      { value: 't', entered: false },
      { value: 'e', entered: false },
      { value: 'n', entered: false },
      { value: 'n', entered: false },
      { value: 'i', entered: false },
      { value: 's', entered: true },
      { value: 't', entered: false },
      { value: 'u', entered: false },
      { value: 't', entered: false },
      { value: 'o', entered: false },
      { value: 'r', entered: false }
    ]
  }
];

describe('Anagram Helper', () => {
  test('marks the correct letters as entered', () => {
    const sort = (x: IExpectedResult) => x.value + x.entered.toString();

    TestCases.forEach((testCase: ITestCase) => {
      const entries = testCase.entries.map((e: string) => ({ value: e }));
      const result = AnagramHelper.shuffleWord(testCase.word, entries);

      expect(sortBy(result, sort)).toEqual(sortBy(testCase.expected, sort));
    });
  });
});
