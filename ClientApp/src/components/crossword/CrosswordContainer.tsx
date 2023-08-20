import React, { Component } from 'react';
import { connect } from 'react-redux';
import { crosswordActions } from './ducks/actions';
import { IStoreState } from '../../state/store';
import { ICrosswordContainerState } from './types';
import Crossword from './crosswords/crossword';

class CrosswordContainer extends Component<any, ICrosswordContainerState> {
  constructor(props: any) {
    super(props);
    this.getCrossword = this.getCrossword.bind(this);
  }

  getCrossword() {
    this.props.get();
  }

  render() {
    const {
      data,
      loading
      // error
    } = this.props;

    if (loading) {
      return null;
    }

    return (
      <div>
        {/* {error} */}
        {/* {JSON.stringify(data)} */}
        <Crossword data={data} />
        <button type="button" className="btn btn-primary" onClick={this.getCrossword}>
          Get crossword
        </button>
      </div>
    );
  }
}

const mapStateToProps = (state: IStoreState): ICrosswordContainerState => {
  const { crossword } = state;
  const { data } = crossword;
  const { loading } = crossword;
  const { error } = crossword;
  return {
    data,
    loading,
    error
  };
};

// inject methods *and* dispatch
const mapDispatchToProps = (dispatch: any) => {
  return {
    dispatch,
    get: () => dispatch(crosswordActions.get())
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(CrosswordContainer);
