import React, { PureComponent } from 'react';
import { ConfirmButton } from './confirm-button';
import Crossword from './crossword';
import { IClue } from '../types';

const buttonClassName = 'button--crossword button--primary';
const buttonCurrentClassName = 'button--crossword--current';
const buttonGenericClassName = 'button--secondary';

export interface IControlProps {
  hasSolutions: boolean;
  clueInFocus: IClue;
  crossword: Crossword;
}

class Controls extends PureComponent<IControlProps> {
  render(): React.ReactNode {
    const { hasSolutions } = this.props;
    const { clueInFocus } = this.props;

    const controls = {
      clue: [] as React.ReactNode[],
      grid: [] as React.ReactNode[]
    };

    // GRID CONTROLS
    controls.grid.unshift(
      <ConfirmButton
        className={`${buttonClassName} ${buttonGenericClassName}`}
        onClick={this.props.crossword.onClearAll.bind(this.props.crossword)}
        key="clear"
        data-link-name="Clear all"
        text="Clear all"
      />
    );

    if (hasSolutions) {
      controls.grid.unshift(
        <ConfirmButton
          className={`${buttonClassName} ${buttonGenericClassName}`}
          onClick={this.props.crossword.onSolution.bind(this.props.crossword)}
          key="solution"
          data-link-name="Reveal all"
          text="Reveal all"
        />
      );
      controls.grid.unshift(
        <ConfirmButton
          className={`${buttonClassName} ${buttonGenericClassName}`}
          onClick={this.props.crossword.onCheckAll.bind(this.props.crossword)}
          key="checkAll"
          data-link-name="Check all"
          text="Check all"
        />
      );
    }

    // HIGHLIGHTED CLUE CONTROLS  - published solution
    if (clueInFocus) {
      controls.clue.unshift(
        <button
          type="button"
          className={`${buttonClassName} ${buttonCurrentClassName}`}
          onClick={this.props.crossword.onClearSingle.bind(this.props.crossword)}
          key="clear-single"
          data-link-name="Clear this">
          Clear this
        </button>
      );

      // anagram helper
      controls.clue.push(
        <button
          type="button"
          className={`${buttonClassName} ${buttonCurrentClassName}`}
          onClick={this.props.crossword.onToggleAnagramHelper.bind(this.props.crossword)}
          key="anagram"
          data-link-name="Show anagram helper">
          Anagram helper
        </button>
      );

      if (hasSolutions) {
        controls.clue.unshift(
          <button
            type="button"
            className={`${buttonClassName} ${buttonCurrentClassName}`}
            onClick={this.props.crossword.onCheat.bind(this.props.crossword)}
            key="cheat"
            data-link-name="Reveal this">
            Reveal this
          </button>
        );
        controls.clue.unshift(
          <button
            type="button"
            className={`${buttonClassName} ${buttonCurrentClassName}`}
            onClick={this.props.crossword.onCheck.bind(this.props.crossword)}
            key="check"
            data-link-name="Check this">
            Check this
          </button>
        );
      }
    }

    return (
      <div className="crossword__controls">
        <div className="crossword__controls__clue">{controls.clue}</div>
        <div className="crossword__controls__grid">{controls.grid}</div>
      </div>
    );
  }
}

export { Controls };
