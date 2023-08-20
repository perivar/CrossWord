import React, { Component } from 'react';
import { IClue } from '../../types';

// Checks a object in the form{",":[4,7]}
const checkIfLetterHasSeparator = (locations: any, letterIndex: number): string => {
  const spaces = locations[','];
  const letterHasBoundary = (separators: any) => separators.includes(letterIndex);

  if (spaces && letterHasBoundary(spaces)) {
    return 'crossword__anagram-helper__cell crossword__anagram-helper__cell--with-space';
  }

  const dashes = locations['-'];
  if (dashes && letterHasBoundary(dashes)) {
    return 'crossword__anagram-helper__cell crossword__anagram-helper__cell--with-hyphen';
  }

  return 'crossword__anagram-helper__cell';
};

export interface ILetter {
  value: string;
  entered?: boolean;
}

export interface ICluePreview {
  isAnimating: boolean;
  isEditable: boolean;
  isError: boolean;
  isHighlighted: boolean;
  key: string;
  number: number;
  value: string;
  solved: boolean;
}

export interface ICluePreviewProps {
  letters: ILetter[];
  entries: ICluePreview[];
  hasShuffled: boolean;
  clue: IClue;
}

class CluePreview extends Component<ICluePreviewProps> {
  /**
   * Get the entries for the preview cells: first filter the user's input to
   * remove anything anything that's already been entered into the grid.
   *
   * With that, we map over the entries, and wherever there's an empty space
   * we insert one of the shuffled input characters.
   *
   * If the user hasn't yet clicked 'shuffle' (this.props.hasShuffled) just
   * display the entries as they are, preserving any blank spaces.
   */
  getEntries(): ICluePreview[] {
    const unsolved = this.props.letters.filter((l: ILetter) => !l.entered);

    return this.props.entries.map((entry: ICluePreview) => {
      entry.solved = !!entry.value;

      const returnVal = this.props.hasShuffled ? (entry.value && entry) || unsolved.shift() : entry;

      return { ...entry, ...returnVal, key: entry.key };
    });
  }

  render() {
    const entries = this.getEntries();

    return (
      <div className={`crossword__anagram-helper__clue-preview ${entries.length >= 9 ? 'long' : ''}`}>
        <div className="crossword__anagram-helper__clue-preview__text">
          <strong>
            {this.props.clue.number} &nbsp; &nbsp;
            <span className="crossword__anagram-helper__direction">{this.props.clue.direction}</span>
          </strong>{' '}
          {this.props.clue.clue}
        </div>
        {entries.map((entry: ICluePreview, i: number) => {
          const classNames = checkIfLetterHasSeparator(this.props.clue.separatorLocations, i + 1); // Separators are one indexed in CAPI, annoyingly

          return (
            <span className={classNames + (entry.solved ? ' has-value' : '')} key={entry.key}>
              {entry.value || ''}
            </span>
          );
        })}
      </div>
    );
  }
}

export { CluePreview };
