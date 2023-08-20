export interface IForecast {
  dateFormatted: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

export interface IForecastProps {
  forecasts: IForecast[];
  loading: boolean;
  loadError: string | null;
  getForecasts: () => void;
}

export interface IForecastState {
  forecasts: IForecast[];
  loading: boolean;
  loadError: string | null;
}
