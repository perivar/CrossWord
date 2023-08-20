import React, { Component } from 'react';
import Crossword from './crossword';

export interface IHiddenInputProps {
  value: string;
  crossword: Crossword;
}

export interface IHiddenInputState {
  value: string;
}

class HiddenInput extends Component<IHiddenInputProps, IHiddenInputState> {
  // make sure the ref objects are public
  public wrapper: React.RefObject<HTMLDivElement>;
  public input: React.RefObject<HTMLInputElement>;

  constructor(props: IHiddenInputProps) {
    super(props);
    this.state = {
      value: this.props.value
    };

    this.wrapper = React.createRef();
    this.input = React.createRef();
  }

  handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.props.crossword.insertCharacter(event.target.value);
    this.setState({
      value: ''
    });
  }

  onClick(event: React.MouseEvent<HTMLInputElement, MouseEvent>) {
    this.props.crossword.onClickHiddenInput(event);
  }

  onKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    this.props.crossword.onKeyDown(event);
  }

  onBlur(event: React.FocusEvent<HTMLInputElement>) {
    this.props.crossword.goToReturnPosition(event);
  }

  onFocusPrevious() {
    this.props.crossword.focusPreviousClue();
  }

  onFocusNext() {
    this.props.crossword.focusNextClue();
  }

  touchStart(event: React.TouchEvent<HTMLInputElement>) {
    this.props.crossword.onClickHiddenInput(event);
  }

  render() {
    return (
      <div className="crossword__hidden-input-wrapper" ref={this.wrapper}>
        <input
          key="1"
          type="text"
          className="crossword__hidden-input-prev-next"
          onFocus={this.onFocusPrevious.bind(this)}
        />
        <input
          key="2"
          type="text"
          className="crossword__hidden-input"
          maxLength={1}
          onClick={this.onClick.bind(this)}
          onChange={this.handleChange.bind(this)}
          onTouchStart={this.touchStart.bind(this)}
          onKeyDown={this.onKeyDown.bind(this)}
          onBlur={this.onBlur.bind(this)}
          value={this.state.value}
          autoComplete="off"
          spellCheck={false}
          autoCorrect="off"
          ref={this.input}
        />
        <input
          key="3"
          type="text"
          className="crossword__hidden-input-prev-next"
          onFocus={this.onFocusNext.bind(this)}
        />
      </div>
    );
  }
}

export { HiddenInput };
