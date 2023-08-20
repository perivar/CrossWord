import React, { Component } from 'react';
import { findDOMNode } from 'react-dom';
import { IClue } from '../../types';

export interface IClueInputProps {
  value: string;
  clue: IClue;
  onChange: (value: string) => void;
  onEnter: () => void;
}

class ClueInput extends Component<IClueInputProps> {
  componentDidMount() {
    const el = findDOMNode(this) as HTMLInputElement;
    if (el) {
      el.focus();
    }
  }

  componentDidUpdate() {
    const el = findDOMNode(this) as HTMLInputElement;

    // focus on reset
    if (this.props.value === '' && el) {
      el.focus();
    }
  }

  onInputChange(e: React.ChangeEvent<HTMLInputElement>) {
    if (!(e.target instanceof HTMLInputElement)) {
      return;
    }
    this.props.onChange(e.target.value.toLowerCase());
  }

  onKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    const el = findDOMNode(this) as HTMLInputElement;

    if (e.keyCode === 13 && el) {
      el.blur();
      this.props.onEnter();
    }
  }

  render() {
    return (
      <input
        type="text"
        className="crossword__anagram-helper__clue-input"
        placeholder="Enter letters"
        maxLength={this.props.clue.length}
        value={this.props.value}
        onChange={this.onInputChange.bind(this)}
        onKeyDown={this.onKeyDown.bind(this)}
      />
    );
  }
}

export { ClueInput };
