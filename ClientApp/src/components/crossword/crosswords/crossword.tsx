import '../scss/main.scss';
import React, { Component } from 'react';
import debounce from 'lodash/debounce';
import fastdom from 'fastdom';
import zip from 'lodash/zip';
import {
  buildClueMap,
  buildGrid,
  otherDirection,
  entryHasCell,
  cluesFor,
  mapGrid,
  getClearableCellsForClue,
  getLastCellInClue,
  getPreviousClueInGroup,
  isFirstCellInClue,
  getNextClueInGroup,
  isLastCellInClue,
  gridSize,
  checkClueHasBeenAnswered,
  buildSeparatorMap,
  cellsForEntry
} from './helpers';

import { saveGridState, loadGridState } from './persistence';
import { IClue, IPosition, IGrid, ICell, Direction, IClueMap, ICrosswordData } from '../types';

import { AnagramHelper } from './anagram-helper/main';
import { Clues, IClueEntry } from './clues';
import { Controls } from './controls';
import { Grid, IGridProps } from './grid';
import { HiddenInput } from './hidden-input';
import { addMyEventListener } from '../lib/events';
import { classNames } from './classNames';
import { isBreakpoint, isIOS } from '../lib/detect';
import { keycodes } from './keycodes';
import { scrollTo } from '../lib/scroller';

export interface IMove {
  x: number;
  y: number;
  value: string;
  previousValue: string;
}

export interface ICrosswordProps {
  data: ICrosswordData;
  loadGrid: (id: string) => string[][];
  onMove: (move: IMove) => void;
  saveGrid: (id: string, entries: string[][]) => void;
}

export interface ICrosswordState {
  grid: IGrid;
  cellInFocus?: IPosition;
  directionOfEntry?: Direction;
  showAnagramHelper: boolean;
}

class Crossword extends Component<ICrosswordProps, ICrosswordState> {
  // ref variables
  private game: React.RefObject<HTMLDivElement>;
  private stickyClueWrapper: React.RefObject<HTMLDivElement>;
  private gridWrapper: React.RefObject<HTMLDivElement>;
  private hiddenInputComponent: React.RefObject<HiddenInput>;
  private grid: React.RefObject<ICell>;

  // instance variables
  private columns: number;
  private rows: number;
  private clueMap: IClueMap;
  private returnPosition: number;
  private gridHeightIsSet: boolean;

  static defaultProps: ICrosswordProps = {
    data: {
      id: '',
      number: 0,
      name: '',
      creator: { name: '', webUrl: '' },
      date: 0,
      entries: new Array<IClue>(),
      solutionAvailable: false,
      dateSolutionAvailable: 0,
      dimensions: { rows: 0, cols: 0 },
      crosswordType: 'quick',
      pdf: '',
      instructions: ''
    },
    loadGrid: (id: string) => loadGridState(id),
    onMove: () => undefined,
    saveGrid: (id: string, grid: string[][]) => saveGridState(id, grid)
  };

  constructor(props: ICrosswordProps) {
    super(props);
    const { dimensions } = this.props.data;

    this.columns = dimensions.cols;
    this.rows = dimensions.rows;
    this.clueMap = buildClueMap(this.props.data.entries);

    this.state = {
      grid: buildGrid(
        dimensions.rows,
        dimensions.cols,
        this.props.data.entries,
        this.props.loadGrid(this.props.data.id)
      ),
      cellInFocus: undefined,
      directionOfEntry: undefined,
      showAnagramHelper: false
    };

    this.game = React.createRef();
    this.stickyClueWrapper = React.createRef();
    this.gridWrapper = React.createRef();
    this.hiddenInputComponent = React.createRef();
    this.grid = React.createRef();

    // init instance variables
    this.returnPosition = 0;
    this.gridHeightIsSet = false;
  }

  componentDidMount() {
    // mediator.on(
    //   'window:resize',
    //   debounce(this.setGridHeight.bind(this), 200),
    // );

    // mediator.on(
    //   'window:orientationchange',
    //   debounce(this.setGridHeight.bind(this), 200),
    // );

    const delayedSetGridCallback = debounce(this.setGridHeight.bind(this), 200);
    // window.addEventListener('resize', delayedSetGridCallback);
    // window.addEventListener('orientationchange', delayedSetGridCallback);
    addMyEventListener(window, 'resize', delayedSetGridCallback);
    addMyEventListener(window, 'orientationchange', delayedSetGridCallback);

    this.setGridHeight();

    const delayedHandleScrollCallback = debounce(this.handleScroll.bind(this), 200);
    addMyEventListener(window, 'scroll', delayedHandleScrollCallback);

    // read the hash from current location (i.e. current url) i.e. /current_pathname/#clue_id
    // and remove the # to keep only the clue id
    const entryId = window.location.hash.replace('#', '');
    this.focusFirstCellInClueById(entryId);
  }

  componentDidUpdate(prevProps: ICrosswordProps, prevState: ICrosswordState): void {
    // return focus to active cell after exiting anagram helper
    if (!this.state.showAnagramHelper && this.state.showAnagramHelper !== prevState.showAnagramHelper) {
      this.focusCurrentCell();
    }
  }

  onKeyDown(event: React.KeyboardEvent<HTMLInputElement>): void {
    const cell = this.state.cellInFocus;

    if (!event.metaKey && !event.ctrlKey && !event.altKey) {
      if (event.keyCode === keycodes.backspace || event.keyCode === keycodes.delete) {
        event.preventDefault();
        if (cell) {
          if (this.cellIsEmpty(cell.x, cell.y)) {
            this.focusPrevious();
          } else {
            this.setCellValue(cell.x, cell.y, '');
            this.saveGrid();
          }
        }
      } else if (event.keyCode === keycodes.left) {
        event.preventDefault();
        this.moveFocus(-1, 0);
      } else if (event.keyCode === keycodes.up) {
        event.preventDefault();
        this.moveFocus(0, -1);
      } else if (event.keyCode === keycodes.right) {
        event.preventDefault();
        this.moveFocus(1, 0);
      } else if (event.keyCode === keycodes.down) {
        event.preventDefault();
        this.moveFocus(0, 1);
      }
    }
  }

  // called when cell is selected (by click or programtically focused)
  onSelect(x: number, y: number): void {
    const { cellInFocus } = this.state;
    const clue = cluesFor(this.clueMap, x, y);
    const focusedClue = this.clueInFocus();
    let newDirection: Direction;

    const isInsidefocusedClue = (): boolean => (focusedClue ? entryHasCell(focusedClue, x, y) : false);

    if (cellInFocus && cellInFocus.x === x && cellInFocus.y === y && this.state.directionOfEntry) {
      // User has clicked again on the highlighted cell, meaning we ought to swap direction
      newDirection = otherDirection(this.state.directionOfEntry);

      if (clue[newDirection]) {
        this.focusClue(x, y, newDirection);
      }
    } else if (isInsidefocusedClue() && this.state.directionOfEntry) {
      // If we've clicked inside the currently highlighted clue, then we ought to just shift the cursor
      //  to the new cell, not change direction or anything funny.
      this.focusClue(x, y, this.state.directionOfEntry);
    } else {
      this.setState({
        cellInFocus: {
          x,
          y
        }
      });

      const isStartOfClue = (sourceClue: IClue | undefined): boolean =>
        !!sourceClue && sourceClue.position.x === x && sourceClue.position.y === y;

      // If the user clicks on the start of a down clue midway through an across clue, we should
      //  prefer to highlight the down clue.
      if (!isStartOfClue(clue.across) && isStartOfClue(clue.down)) {
        newDirection = 'down';
      } else if (clue.across) {
        // Across is the default focus otherwise
        newDirection = 'across';
      } else {
        newDirection = 'down';
      }
      this.focusClue(x, y, newDirection);
    }
  }

  onCheat(): void {
    this.allHighlightedClues().forEach((clue: IClue) => this.cheat(clue));
    this.saveGrid();
  }

  onCheck(): void {
    // 'Check this' checks single and grouped clues
    this.allHighlightedClues().forEach((clue: IClue) => this.check(clue));
    this.saveGrid();
  }

  onSolution(): void {
    this.props.data.entries.forEach((clue: IClue) => this.cheat(clue));
    this.saveGrid();
  }

  onCheckAll(): void {
    this.props.data.entries.forEach((clue: IClue) => this.check(clue));
    this.saveGrid();
  }

  onClearAll(): void {
    this.setState((prevState) => ({
      grid: mapGrid(prevState.grid, (cell: ICell, gridX: number, gridY: number) => {
        const previousValue = cell.value;
        cell.value = '';
        this.props.onMove({
          x: gridX,
          y: gridY,
          value: '',
          previousValue
        });
        return cell;
      })
    }));

    this.saveGrid();
  }

  onClearSingle(): void {
    const clueInFocus = this.clueInFocus();

    if (clueInFocus) {
      // Merge arrays of cells from all highlighted clues
      // const cellsInFocus = _.flatten(_.map(this.allHighlightedClues(), helpers.cellsForEntry, this));
      const cellsInFocus = getClearableCellsForClue(
        this.state.grid,
        this.clueMap,
        this.props.data.entries,
        clueInFocus
      );

      this.setState((prevState) => ({
        grid: mapGrid(prevState.grid, (cell: any, gridX: number, gridY: number) => {
          if (cellsInFocus.some((c: any) => c.x === gridX && c.y === gridY)) {
            const previousValue = cell.value;
            cell.value = '';
            this.props.onMove({
              x: gridX,
              y: gridY,
              value: '',
              previousValue
            });
          }
          return cell;
        })
      }));

      this.saveGrid();
    }
  }

  onToggleAnagramHelper(): void {
    // only show anagram helper if a clue is active
    if (!this.state.showAnagramHelper) {
      if (this.clueInFocus()) {
        this.setState({
          showAnagramHelper: true
        });
      }
    } else {
      this.setState({
        showAnagramHelper: false
      });
    }
  }

  // onClickHiddenInput(event: React.TouchEvent<HTMLInputElement>): void;
  // onClickHiddenInput(event: React.MouseEvent<HTMLInputElement, MouseEvent>): void;
  onClickHiddenInput(event: any): void {
    const focused = this.state.cellInFocus;

    if (focused) {
      this.onSelect(focused.x, focused.y);
    }

    // We need to handle touch seperately as touching an input on iPhone does not fire the
    // click event - listen for a touchStart and preventDefault to avoid calling onSelect twice on
    // devices that fire click AND touch events. The click event doesn't fire only when the input is already focused
    if (event.type === 'touchstart') {
      event.preventDefault();
    }
  }

  setGridHeight(): void {
    const $gridWrapper = this.gridWrapper.current as HTMLDivElement;

    if (!$gridWrapper) {
      return;
    }

    if (
      isBreakpoint({
        max: 'tablet'
      })
    ) {
      fastdom.measure(() => {
        // Our grid is a square, set the height of the grid wrapper
        // to the width of the grid wrapper
        fastdom.mutate(() => {
          $gridWrapper.setAttribute('style', `height: ${$gridWrapper.offsetWidth}px`);
        });
        this.gridHeightIsSet = true;
      });
    } else if (this.gridHeightIsSet) {
      // Remove inline style if tablet and wider
      $gridWrapper.removeAttribute('style');
    }
  }

  setCellValue(x: number, y: number, value: string, triggerOnMoveCallback = true): void {
    this.setState((prevState) => ({
      grid: mapGrid(prevState.grid, (cell: ICell, gridX: number, gridY: number) => {
        if (gridX === x && gridY === y) {
          const previousValue = cell.value;
          cell.value = value;
          cell.isError = false;

          if (triggerOnMoveCallback) {
            this.props.onMove({
              x,
              y,
              value,
              previousValue
            });
          }
        }

        return cell;
      })
    }));
  }

  getCellValue(x: number, y: number): string {
    return this.state.grid[x][y].value;
  }

  setReturnPosition(position: number): void {
    this.returnPosition = position;
  }

  handleScroll(): void {
    // Sticky clue
    const $stickyClueWrapper = this.stickyClueWrapper.current as HTMLDivElement;

    if (!$stickyClueWrapper) {
      return;
    }
    const stickyClueWrapperOffsetHeight = $stickyClueWrapper.offsetHeight;

    const $game = this.game.current as HTMLDivElement;
    const gameOffsetTop = $game.offsetTop;
    const gameOffsetHeight = $game.offsetHeight;

    // const $grid = this.grid.current as React.ReactNode;
    // const gridOffsetTop = $grid.offsetTop;
    // const gridOffsetHeight = $grid.offsetHeight;

    const { scrollY } = window;

    fastdom.mutate(() => {
      // Clear previous state
      $stickyClueWrapper.removeAttribute('style');
      $stickyClueWrapper.classList.remove('is-fixed');

      const scrollYPastGame = scrollY - gameOffsetTop;

      if (scrollYPastGame >= 0) {
        // const gridOffsetBottom = gridOffsetTop + gridOffsetHeight;
        const gameOffsetBottom = gameOffsetTop + gameOffsetHeight;

        if (scrollY > gameOffsetBottom - stickyClueWrapperOffsetHeight) {
          // if (scrollY > gridOffsetBottom - stickyClueWrapperOffsetHeight) {
          $stickyClueWrapper.setAttribute('style', `top: auto, bottom: 0px`);
        } else if (isIOS()) {
          // iOS doesn't support sticky things when the keyboard
          // is open, so we use absolute positioning and
          // programatically update the value of top
          $stickyClueWrapper.setAttribute('style', `top: ${scrollYPastGame}px`);
        } else {
          $stickyClueWrapper.classList.add('is-fixed');
        }
      }
    });
  }

  updateGrid(gridState: any) {
    this.setState({
      grid: buildGrid(this.rows, this.columns, this.props.data.entries, gridState)
    });
  }

  insertCharacter(character: string) {
    const characterUppercase = character.toUpperCase();
    const cell = this.state.cellInFocus;
    if (/[A-Za-zÀ-ÿ0-9]/.test(characterUppercase) && characterUppercase.length === 1 && cell) {
      this.setCellValue(cell.x, cell.y, characterUppercase);
      this.saveGrid();
      this.focusNext();
    }
  }

  cellIsEmpty(x: number, y: number): boolean {
    return !this.getCellValue(x, y);
  }

  goToReturnPosition(_event: React.FocusEvent<HTMLInputElement>): void {
    if (
      isBreakpoint({
        max: 'mobile'
      })
    ) {
      if (this.returnPosition) {
        scrollTo(this.returnPosition, 250, 'easeOutQuad');
      }
      this.returnPosition = 0;
    }
  }

  indexOfClueInFocus(): number {
    return this.props.data.entries.indexOf(this.clueInFocus() as IClue);
  }

  focusPreviousClue(): void {
    const i = this.indexOfClueInFocus();
    const { entries } = this.props.data;

    if (i !== -1) {
      const newClue = entries[i === 0 ? entries.length - 1 : i - 1];
      this.focusClue(newClue.position.x, newClue.position.y, newClue.direction);
    }
  }

  focusNextClue(): void {
    const i = this.indexOfClueInFocus();
    const { entries } = this.props.data;

    if (i !== -1) {
      const newClue = entries[i === entries.length - 1 ? 0 : i + 1];
      this.focusClue(newClue.position.x, newClue.position.y, newClue.direction);
    }
  }

  moveFocus(deltaX: number, deltaY: number): void {
    const cell = this.state.cellInFocus;

    if (!cell) {
      return;
    }

    const x = cell.x + deltaX;
    const y = cell.y + deltaY;
    let direction: Direction = 'down';

    if (this.state.grid[x] && this.state.grid[x][y] && this.state.grid[x][y].isEditable) {
      if (deltaY !== 0) {
        direction = 'down';
      } else if (deltaX !== 0) {
        direction = 'across';
      }
      this.focusClue(x, y, direction);
    }
  }

  isAcross(): boolean {
    return this.state.directionOfEntry === 'across';
  }

  focusPrevious(): void {
    const cell = this.state.cellInFocus;
    const clue = this.clueInFocus();

    if (cell && clue) {
      if (isFirstCellInClue(cell, clue)) {
        const newClue = getPreviousClueInGroup(this.props.data.entries, clue);
        if (newClue) {
          const newCell = getLastCellInClue(newClue) as IPosition;
          this.focusClue(newCell.x, newCell.y, newClue.direction);
        }
      } else if (this.isAcross()) {
        this.moveFocus(-1, 0);
      } else {
        this.moveFocus(0, -1);
      }
    }
  }

  focusNext(): void {
    const cell = this.state.cellInFocus;
    const clue = this.clueInFocus();

    if (cell && clue) {
      if (isLastCellInClue(cell, clue)) {
        const newClue = getNextClueInGroup(this.props.data.entries, clue);
        if (newClue) {
          this.focusClue(newClue.position.x, newClue.position.y, newClue.direction);
        }
      } else if (this.isAcross()) {
        this.moveFocus(1, 0);
      } else {
        this.moveFocus(0, 1);
      }
    }
  }

  asPercentage(x: number, y: number): IPosition {
    const width = gridSize(this.columns);
    const height = gridSize(this.rows);

    return {
      x: (100 * x) / width,
      y: (100 * y) / height
    };
  }

  focusHiddenInput(x: number, y: number): void {
    const hiddenNode = this.hiddenInputComponent.current as HiddenInput;
    const hiddenWrapperNode = hiddenNode.wrapper.current as HTMLDivElement;

    const left = gridSize(x);
    const top = gridSize(y);
    const position = this.asPercentage(left, top);

    // This has to be done before focus to move viewport accordingly
    hiddenWrapperNode.style.left = `${position.x}% `;
    hiddenWrapperNode.style.top = `${position.y}% `;

    const hiddenInputNode = hiddenNode.input.current as HTMLDivElement;

    if (document.activeElement !== hiddenInputNode) {
      hiddenInputNode.focus();
    }
  }

  // Focus corresponding clue for a given cell
  focusClue(x: number, y: number, direction: Direction): void {
    const clues = cluesFor(this.clueMap, x, y);
    const clue = clues[direction];

    if (clues && clue) {
      this.focusHiddenInput(x, y);

      this.setState((prevState) => ({
        grid: prevState.grid,
        cellInFocus: {
          x,
          y
        },
        directionOfEntry: direction
      }));

      // update the history (i.e. current url) to /current_pathname/#clue_id
      window.history.replaceState(undefined, document.title, `${window.location.pathname}#${clue.id} `);
    }
  }

  // Focus first cell in given clue
  focusFirstCellInClue(entry: IClue): void {
    this.focusClue(entry.position.x, entry.position.y, entry.direction);
  }

  focusFirstCellInClueById(clueId: string): void {
    const newEntry = this.props.data.entries.find((val: IClue) => val.id === clueId);
    if (newEntry) {
      this.focusFirstCellInClue(newEntry);
    }
  }

  focusCurrentCell(): void {
    if (this.state.cellInFocus) {
      this.focusHiddenInput(this.state.cellInFocus.x, this.state.cellInFocus.y);
    }
  }

  clueInFocus(): IClue | null {
    if (this.state.cellInFocus) {
      const cluesForCell = cluesFor(this.clueMap, this.state.cellInFocus.x, this.state.cellInFocus.y);

      if (this.state.directionOfEntry) {
        return cluesForCell[this.state.directionOfEntry] as IClue;
      }
    }
    return null;
  }

  allHighlightedClues(): IClue[] {
    return this.props.data.entries.filter((clue: IClue) => this.clueIsInFocusGroup(clue));
  }

  clueIsInFocusGroup(clue: IClue): boolean {
    if (this.state.cellInFocus) {
      const cluesForCell = cluesFor(this.clueMap, this.state.cellInFocus.x, this.state.cellInFocus.y);

      if (this.state.directionOfEntry && cluesForCell[this.state.directionOfEntry]) {
        const cluesIntersect = cluesForCell[this.state.directionOfEntry] as IClue;
        return cluesIntersect.group.includes(clue.id);
      }
    }
    return false;
  }

  cluesData(): IClueEntry[] {
    return this.props.data.entries.map((entry: IClue) => {
      const hasAnswered = checkClueHasBeenAnswered(this.state.grid, entry);
      return {
        entry,
        hasAnswered,
        isSelected: this.clueIsInFocusGroup(entry)
      };
    });
  }

  cheat(entry: IClue): void {
    const cells = cellsForEntry(entry);

    if (entry.solution) {
      this.setState((prevState) => ({
        grid: mapGrid(prevState.grid, (cell: any, x: number, y: number) => {
          if (cells.some((c: IPosition) => c.x === x && c.y === y)) {
            const n = entry.direction === 'across' ? x - entry.position.x : y - entry.position.y;
            const previousValue = cell.value;
            cell.value = entry.solution[n];
            this.props.onMove({
              x,
              y,
              value: cell.value,
              previousValue
            });
          }

          return cell;
        })
      }));
    }
  }

  check(entry: IClue): void {
    const cells = cellsForEntry(entry);

    if (entry.solution) {
      const badCells = zip(cells, entry.solution.split(''))
        .filter((cellAndSolution: any) => {
          const coords = cellAndSolution[0];
          const cell = this.state.grid[coords.x][coords.y];
          const solution = cellAndSolution[1];
          return /^.$/.test(cell.value) && cell.value !== solution;
        })
        .map((cellAndSolution: any) => cellAndSolution[0]);

      this.setState((prevState) => ({
        grid: mapGrid(prevState.grid, (cell: any, gridX: number, gridY: number) => {
          if (badCells.some((bad: IPosition) => bad.x === gridX && bad.y === gridY)) {
            const previousValue = cell.value;
            cell.value = '';
            this.props.onMove({
              x: gridX,
              y: gridY,
              value: '',
              previousValue
            });
          }

          return cell;
        })
      }));
    }
  }

  hiddenInputValue(): string {
    const cell = this.state.cellInFocus;

    let currentValue;

    if (cell) {
      currentValue = this.state.grid[cell.x][cell.y].value;
    }

    return currentValue || '';
  }

  hasSolutions(): boolean {
    if (!!this.props.data.entries && this.props.data.entries.length > 0) {
      return 'solution' in this.props.data.entries[0];
    }
    return false;
  }

  isHighlighted(x: number, y: number): boolean {
    const focused = this.clueInFocus();
    return focused
      ? focused.group.some((id: string) => {
          const entry = this.props.data.entries.find((e: IClue) => e.id === id) as IClue;
          return entryHasCell(entry, x, y);
        })
      : false;
  }

  saveGrid(): void {
    const entries = this.state.grid.map((row: any) => row.map((cell: ICell) => cell.value));
    this.props.saveGrid(this.props.data.id, entries);
  }

  render() {
    const focused = this.clueInFocus() as IClue;

    const anagramHelper = this.state.showAnagramHelper && (
      <AnagramHelper
        key={focused.id}
        crossword={this}
        focusedEntry={focused}
        entries={this.props.data.entries}
        grid={this.state.grid}
        close={this.onToggleAnagramHelper}
      />
    );

    const gridProps: IGridProps = {
      rows: this.rows,
      columns: this.columns,
      cells: this.state.grid,
      separators: buildSeparatorMap(this.props.data.entries),
      crossword: this,
      focusedCell: this.state.cellInFocus,
      ref: this.grid
    };

    return (
      <div
        className={`crossword__container crossword__container--${this.props.data.crosswordType} crossword__container--react`}
        data-link-name="Crosswords">
        <div className="crossword__container__game" ref={this.game}>
          <div className="crossword__sticky-clue-wrapper" ref={this.stickyClueWrapper}>
            <div
              className={classNames({
                'crossword__sticky-clue': true,
                'is-hidden': !focused
              })}>
              {focused && (
                <div className="crossword__sticky-clue__inner">
                  <div className="crossword__sticky-clue__inner__inner">
                    <strong>
                      {focused.number} <span className="crossword__sticky-clue__direction">{focused.direction}</span>
                    </strong>{' '}
                    {focused.clue}
                  </div>
                </div>
              )}
            </div>
          </div>
          <div className="crossword__container__grid-wrapper" ref={this.gridWrapper}>
            {Grid(gridProps)}
            <HiddenInput crossword={this} value={this.hiddenInputValue()} ref={this.hiddenInputComponent} />
            {anagramHelper}
          </div>
        </div>
        <Controls hasSolutions={this.hasSolutions()} clueInFocus={focused} crossword={this} />
        <Clues
          clues={this.cluesData()}
          focused={focused}
          focusFirstCellInClueById={this.focusFirstCellInClueById.bind(this)}
          setReturnPosition={this.setReturnPosition.bind(this)}
        />
      </div>
    );
  }
}

export default Crossword;
