import { applyMiddleware, combineReducers, createStore } from 'redux';
import { composeWithDevTools } from '@redux-devtools/extension';
import thunk from 'redux-thunk';
import { logger } from './logger';
import { jwt } from '../components/auth/middleware';

import forecastReducer from '../components/forecast/ducks/reducers';
import alertReducer from '../components/alert/ducks/reducers';
import authenticationReducer from '../components/auth/ducks/reducers';
import crosswordReducer from '../components/crossword/ducks/reducers';
import { IAlertState, IAuthState, IRegisterState, IUserState } from '../components/auth/types';
import { IForecastState } from '../components/forecast/types';
import { ICrosswordContainerState } from '../components/crossword/types';

const reducers = {
  ...forecastReducer,
  ...alertReducer,
  ...authenticationReducer,
  ...crosswordReducer
};

const initialCrosswordState: ICrosswordContainerState = {
  data: {
    id: 'crosswords/quick/15315',
    number: 15315,
    name: 'Quick crossword No 15,315',
    creator: { name: 'Guardian', webUrl: '' },
    date: 1559952000000,
    entries: [
      {
        id: '1-across',
        number: 1,
        humanNumber: '1',
        clue: 'Ear membrane — my MP a nut (anag) (8)',
        direction: 'across',
        length: 8,
        group: ['1-across'],
        position: {
          x: 0,
          y: 0
        },
        separatorLocations: {},
        solution: 'TYMPANUM'
      },
      {
        id: '5-across',
        number: 5,
        humanNumber: '5',
        clue: 'Sudden sharp feeling (4)',
        direction: 'across',
        length: 4,
        group: ['5-across'],
        position: {
          x: 9,
          y: 0
        },
        separatorLocations: {},
        solution: 'PANG'
      },
      {
        id: '9-across',
        number: 9,
        humanNumber: '9',
        clue: 'Tycoon — ski slope mound (5)',
        direction: 'across',
        length: 5,
        group: ['9-across'],
        position: {
          x: 0,
          y: 2
        },
        separatorLocations: {},
        solution: 'MOGUL'
      },
      {
        id: '10-across',
        number: 10,
        humanNumber: '10',
        clue: 'Gulf sheikhdom (7)',
        direction: 'across',
        length: 7,
        group: ['10-across'],
        position: {
          x: 6,
          y: 2
        },
        separatorLocations: {},
        solution: 'BAHRAIN'
      },
      {
        id: '11-across',
        number: 11,
        humanNumber: '11',
        clue: 'Grouchy (12)',
        direction: 'across',
        length: 12,
        group: ['11-across'],
        position: {
          x: 1,
          y: 4
        },
        separatorLocations: {},
        solution: 'CANTANKEROUS'
      },
      {
        id: '13-across',
        number: 13,
        humanNumber: '13',
        clue: 'Expresses audibly (6)',
        direction: 'across',
        length: 6,
        group: ['13-across'],
        position: {
          x: 0,
          y: 6
        },
        separatorLocations: {},
        solution: 'UTTERS'
      },
      {
        id: '14-across',
        number: 14,
        humanNumber: '14',
        clue: 'Calm (6)',
        direction: 'across',
        length: 6,
        group: ['14-across'],
        position: {
          x: 7,
          y: 6
        },
        separatorLocations: {},
        solution: 'SEDATE'
      },
      {
        id: '17-across',
        number: 17,
        humanNumber: '17',
        clue: 'Dinosaur with a long neck and tail (12)',
        direction: 'across',
        length: 12,
        group: ['17-across'],
        position: {
          x: 0,
          y: 8
        },
        separatorLocations: {},
        solution: 'BRONTOSAURUS'
      },
      {
        id: '20-across',
        number: 20,
        humanNumber: '20',
        clue: 'In the open air (7)',
        direction: 'across',
        length: 7,
        group: ['20-across'],
        position: {
          x: 0,
          y: 10
        },
        separatorLocations: {},
        solution: 'OUTDOOR'
      },
      {
        id: '21-across',
        number: 21,
        humanNumber: '21',
        clue: 'Dirty (5)',
        direction: 'across',
        length: 5,
        group: ['21-across'],
        position: {
          x: 8,
          y: 10
        },
        separatorLocations: {},
        solution: 'GRIMY'
      },
      {
        id: '22-across',
        number: 22,
        humanNumber: '22',
        clue: 'Boot (4)',
        direction: 'across',
        length: 4,
        group: ['22-across'],
        position: {
          x: 0,
          y: 12
        },
        separatorLocations: {},
        solution: 'KICK'
      },
      {
        id: '23-across',
        number: 23,
        humanNumber: '23',
        clue: 'No 1 in the periodic table (8)',
        direction: 'across',
        length: 8,
        group: ['23-across'],
        position: {
          x: 5,
          y: 12
        },
        separatorLocations: {},
        solution: 'HYDROGEN'
      },
      {
        id: '1-down',
        number: 1,
        humanNumber: '1',
        clue: 'Hours clocked up? (4)',
        direction: 'down',
        length: 4,
        group: ['1-down'],
        position: {
          x: 0,
          y: 0
        },
        separatorLocations: {},
        solution: 'TIME'
      },
      {
        id: '2-down',
        number: 2,
        humanNumber: '2',
        clue: 'Person moving abroad (7)',
        direction: 'down',
        length: 7,
        group: ['2-down'],
        position: {
          x: 2,
          y: 0
        },
        separatorLocations: {},
        solution: 'MIGRANT'
      },
      {
        id: '3-down',
        number: 3,
        humanNumber: '3',
        clue: 'Poetic rhyming device using words with the same initial letter (12)',
        direction: 'down',
        length: 12,
        group: ['3-down'],
        position: {
          x: 4,
          y: 0
        },
        separatorLocations: {},
        solution: 'ALLITERATION'
      },
      {
        id: '4-down',
        number: 4,
        humanNumber: '4',
        clue: 'Cultured (6)',
        direction: 'down',
        length: 6,
        group: ['4-down'],
        position: {
          x: 6,
          y: 0
        },
        separatorLocations: {},
        solution: 'URBANE'
      },
      {
        id: '6-down',
        number: 6,
        humanNumber: '6',
        clue: 'Where Davy Crockett died, 1836 (5)',
        direction: 'down',
        length: 5,
        group: ['6-down'],
        position: {
          x: 10,
          y: 0
        },
        separatorLocations: {},
        solution: 'ALAMO'
      },
      {
        id: '7-down',
        number: 7,
        humanNumber: '7',
        clue: 'Hoodlum (8)',
        direction: 'down',
        length: 8,
        group: ['7-down'],
        position: {
          x: 12,
          y: 0
        },
        separatorLocations: {},
        solution: 'GANGSTER'
      },
      {
        id: '8-down',
        number: 8,
        humanNumber: '8',
        clue: 'Fast food — begs cure here (anag) (12)',
        direction: 'down',
        length: 12,
        group: ['8-down'],
        position: {
          x: 8,
          y: 1
        },
        separatorLocations: {},
        solution: 'CHEESEBURGER'
      },
      {
        id: '12-down',
        number: 12,
        humanNumber: '12',
        clue: 'Skin cream to filter out ultraviolet light (8)',
        direction: 'down',
        length: 8,
        group: ['12-down'],
        position: {
          x: 0,
          y: 5
        },
        separatorLocations: {},
        solution: 'SUNBLOCK'
      },
      {
        id: '15-down',
        number: 15,
        humanNumber: '15',
        clue: 'Droll (7)',
        direction: 'down',
        length: 7,
        group: ['15-down'],
        position: {
          x: 10,
          y: 6
        },
        separatorLocations: {},
        solution: 'AMUSING'
      },
      {
        id: '16-down',
        number: 16,
        humanNumber: '16',
        clue: 'Up the garden path? (6)',
        direction: 'down',
        length: 6,
        group: ['16-down'],
        position: {
          x: 6,
          y: 7
        },
        separatorLocations: {},
        solution: 'ASTRAY'
      },
      {
        id: '18-down',
        number: 18,
        humanNumber: '18',
        clue: 'Device for measuring out spirits (5)',
        direction: 'down',
        length: 5,
        group: ['18-down'],
        position: {
          x: 2,
          y: 8
        },
        separatorLocations: {},
        solution: 'OPTIC'
      },
      {
        id: '19-down',
        number: 19,
        humanNumber: '19',
        clue: 'Bluish green (4)',
        direction: 'down',
        length: 4,
        group: ['19-down'],
        position: {
          x: 12,
          y: 9
        },
        separatorLocations: {},
        solution: 'CYAN'
      }
    ],
    solutionAvailable: true,
    dateSolutionAvailable: 1559948400000,
    dimensions: {
      cols: 13,
      rows: 13
    },
    crosswordType: 'quick',
    pdf: 'https://crosswords-static.guim.co.uk/gdn.quick.20190608.pdf',
    instructions: ''
  },
  loading: false,
  error: ''
};

export interface IStoreState {
  alert: IAlertState;
  authentication: IAuthState;
  crossword: ICrosswordContainerState;
  forecast: IForecastState;
  registration: IRegisterState;
  users: IUserState;
}

const configureStore = (): any => {
  // : Store<IStoreState>
  const rootReducer = combineReducers(reducers); // <IStoreState>

  // https://stackoverflow.com/questions/35305661/where-to-write-to-localstorage-in-a-redux-app
  const persistedState = localStorage.getItem('reduxState') ? JSON.parse(localStorage.getItem('reduxState')!) : {};

  const store = createStore<IStoreState, any, any, any>(
    rootReducer,
    {
      ...persistedState,
      crossword: initialCrosswordState // always overwrite the crossword elements with the initial state
    },
    composeWithDevTools(applyMiddleware(jwt, thunk, logger))
  );

  store.subscribe(() => {
    localStorage.setItem('reduxState', JSON.stringify(store.getState()));
  });

  return store;
};

export default configureStore;
