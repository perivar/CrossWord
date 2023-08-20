import { connect } from 'react-redux';
import ForecastComponent from './ForecastComponent';

import * as operations from './ducks/operations';
import { IStoreState } from '../../state/store';

const mapStateToProps = (state: IStoreState) => {
  return {
    forecasts: state.forecast.forecasts,
    loading: state.forecast.loading,
    loadError: state.forecast.loadError
  };
};

const mapDispatchToProps = (dispatch: any) => {
  return {
    getForecasts: () => dispatch(operations.getForecast())
  };
};

const ForecastContainer = connect(mapStateToProps, mapDispatchToProps)(ForecastComponent);

export default ForecastContainer;
