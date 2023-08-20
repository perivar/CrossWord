// https://github.com/guardian/frontend/blob/master/static/src/javascripts/__flow__/types/crosswords.js

export type Direction = 'across' | 'down';

export type Separator = '-' | ',';

export type Axis = 'x' | 'y';

export type SeparatorLocations = {
  [separator in Separator]: number[];
};

export type IPosition = {
  [axis in Axis]: number;
};

export interface ISeparatorDescription {
  direction: Direction;
  separator: Separator;
}

export interface ISeparatorMap {
  [index: string]: ISeparatorDescription;
}

export interface ICell {
  isAnimating: boolean;
  isEditable: boolean;
  isError: boolean;
  isHighlighted: boolean;
  number: number;
  value: string;
}

export type IGrid = ICell[][];

export type CrosswordType =
  | 'cryptic'
  | 'quick'
  | 'quiptic'
  | 'prize'
  | 'everyman'
  | 'azed'
  | 'special'
  | 'genius'
  | 'speedy'
  | 'weekend';

export interface ICreator {
  name: string;
  webUrl: string;
}

export interface ICrosswordData {
  id: string; // crosswords/cryptic/27847
  number: number; // 27847
  name: string; // Cryptic crossword No 27,847
  creator: ICreator; // {"name":"Paul","webUrl":"https://www.theguardian.com/profile/paul"}
  date: number; // 1560470400000
  entries: IClue[]; //
  solutionAvailable: boolean; // true
  dateSolutionAvailable: number; // 1560466800000
  dimensions: IDimensions; // {"cols":15,"rows":15}
  crosswordType: CrosswordType; // cryptic
  pdf: string; // https://crosswords-static.guim.co.uk/gdn.cryptic.20190614.pdf
  instructions: string; //
}

export interface IClue {
  id: string; // '1-across',
  number: number; // 1
  humanNumber: string; // '1'
  clue: string; // 'Toy on a string (2-2)'
  direction: Direction; // 'across'
  length: number; // 4
  group: string[]; // ['1-across']
  position: IPosition; // { x: 0, y: 0 }
  separatorLocations: SeparatorLocations | {}; // { '-': [2] }
  solution: string; // YOYO
}

export interface IGroupClue {
  id: string;
  number: string;
  length: number;
  separatorLocations: SeparatorLocations;
  direction: '';
  clue: string;
}

export interface ICluesIntersect {
  across?: IClue;
  down?: IClue;
}

export interface IClueMap {
  [index: string]: ICluesIntersect;
}

export interface IDimensions {
  cols: number;
  rows: number;
}

export interface ICrosswordContainerState {
  data: ICrosswordData;
  loading: boolean;
  error: string;
}
