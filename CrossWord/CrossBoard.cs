using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrossWord.Models;

namespace CrossWord
{
    public class Coordinate : IEquatable<Coordinate>, IComparable<Coordinate>
    {
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public int GridNumber { get; protected set; }

        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Coordinate(int x, int y, int gridNumber)
        {
            X = x;
            Y = y;
            GridNumber = gridNumber;
        }

        // https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type
        public bool Equals(Coordinate other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            return this.X == other.X && this.Y == other.Y;
        }

        public override bool Equals(object obj) => this.Equals(obj as Coordinate);

        public static bool operator ==(Coordinate obj1, Coordinate obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(Coordinate obj1, Coordinate obj2)
        {
            return !(obj1 == obj2);
        }

        public override int GetHashCode()
        {
            return this.Y + 1000 * this.X;
        }

        public int CompareTo(Coordinate other)
        {
            // sort by Y then X
            int result = this.Y.CompareTo(other.Y);
            if (result == 0)
            {
                result = this.X.CompareTo(other.X);
            }
            return result;
        }

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, GridNumber: {2}", X, Y, GridNumber);
        }
    }

    public class XYStartWordComparer : IComparer<StartWord>
    {
        int IComparer<StartWord>.Compare(StartWord sw1, StartWord sw2)
        {
            int result = sw1.StartX.CompareTo(sw2.StartX);
            if (result == 0)
            {
                result = sw1.StartY.CompareTo(sw2.StartY);
            }
            return result;
        }
    }

    public class YXStartWordComparer : IComparer<StartWord>
    {
        int IComparer<StartWord>.Compare(StartWord sw1, StartWord sw2)
        {
            int result = sw1.StartY.CompareTo(sw2.StartY);
            if (result == 0)
            {
                result = sw1.StartX.CompareTo(sw2.StartX);
            }
            return result;
        }
    }

    public class CrossBoard : ICrossBoard
    {
        int _sizeX;
        int _sizeY;
        List<StartWord> _startWords;

        List<CrossPattern> _horizontalPatterns;
        List<CrossPattern> _verticalPatterns;

        public void SetBoardSize(int aX, int aY)
        {
            _sizeX = aX;
            _sizeY = aY;
            _startWords = new List<StartWord>();

            _horizontalPatterns = null;
            _verticalPatterns = null;
        }

        public void AddStartWord(StartWord aStartWord)
        {
            _startWords.Add(aStartWord);
        }

        public void AddStartWord(int aX, int aY)
        {
            var sw = new StartWord { StartX = aX, StartY = aY };
            AddStartWord(sw);
        }

        const int MinPatternLength = 2;

        public void Preprocess(ICrossDictionary aDict)
        {
            _horizontalPatterns = new List<CrossPattern>();
            _startWords.Sort(new YXStartWordComparer()); // first create horizontal patterns

            int wordIdx = 0;
            for (int y = 0; y < _sizeY; y++)
            {
                int nextX = 0;
                while (wordIdx < _startWords.Count)
                {
                    var sw = _startWords[wordIdx];
                    // Console.WriteLine("StartWord x:{0} y:{1} idx:{2}/cnt:{3}",sw.StartX,sw.StartY,wordIdx,_startWords.Count);
                    if (sw.StartY == y)
                    {
                        if (sw.StartX - nextX >= MinPatternLength)
                        {
                            var cp = new CrossPattern(nextX, y, sw.StartX - nextX, true);
                            // Console.WriteLine("SW pattern startX: {0} startY: {1} len: {2}",cp.StartX, cp.StartY, cp.Length);
                            _horizontalPatterns.Add(cp);
                        }
                        nextX = sw.StartX + 1;
                        wordIdx++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (_sizeX - nextX >= MinPatternLength)
                {
                    var cp = new CrossPattern(nextX, y, _sizeX - nextX, true);
                    // Console.WriteLine("EL pattern startX: {0} startY: {1} len: {2}",cp.StartX, cp.StartY, cp.Length);
                    _horizontalPatterns.Add(cp);
                }
            }

            _verticalPatterns = new List<CrossPattern>();
            _startWords.Sort(new XYStartWordComparer()); // then create vertical patterns

            wordIdx = 0;
            for (int x = 0; x < _sizeX; x++)
            {
                int nextY = 0;
                while (wordIdx < _startWords.Count)
                {
                    var sw = _startWords[wordIdx];
                    // Console.WriteLine("StartWord x:{0} y:{1} idx:{2}/cnt:{3}",sw.StartX,sw.StartY,wordIdx,_startWords.Count);
                    if (sw.StartX == x)
                    {
                        if (sw.StartY - nextY >= MinPatternLength)
                        {
                            var cp = new CrossPattern(x, nextY, sw.StartY - nextY, false);
                            // Console.WriteLine("SW patternY startX: {0} startY: {1} len: {2}",cp.StartX, cp.StartY, cp.Length);
                            _verticalPatterns.Add(cp);
                        }
                        nextY = sw.StartY + 1;
                        wordIdx++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (_sizeY - nextY >= MinPatternLength)
                {
                    var cp = new CrossPattern(x, nextY, _sizeY - nextY, false);
                    // Console.WriteLine("EL patternY startX: {0} startY: {1} len: {2}",cp.StartX, cp.StartY, cp.Length);
                    _verticalPatterns.Add(cp);
                }
            }

            BindAdjacentPatterns();

            // calculate instantiation count
            var wordLengthCount = new int[aDict.MaxWordLength + 1];
            for (int i = 1; i <= aDict.MaxWordLength; i++)
            {
                wordLengthCount[i] = aDict.GetWordOfLengthCount(i);
            }

            int patternCount = GetPatternCount();
            for (int i = 0; i < patternCount; i++)
            {
                var pattern = GetCrossPattern(i);
                if (pattern.Pattern == null)
                {
                    // empty
                    pattern.InstantiationCount = wordLengthCount[pattern.Length];
                    pattern.Pattern = Enumerable.Repeat('.', pattern.Length).ToArray();
                }
                else
                {
                    // already set some letters
                    pattern.InstantiationCount = aDict.GetMatchCount(pattern.Pattern);
                }
            }
        }

        void BindAdjacentPatterns()
        {
            foreach (var hor in _horizontalPatterns)
            {
                foreach (var ver in _verticalPatterns)
                {
                    if (ver.StartX >= hor.StartX && ver.StartX < hor.StartX + hor.Length &&
                        hor.StartY >= ver.StartY && hor.StartY < ver.StartY + ver.Length)
                    {
                        // adjacent
                        hor.AdjacentPatterns[ver.StartX - hor.StartX] = ver;
                        ver.AdjacentPatterns[hor.StartY - ver.StartY] = hor;

                        // Console.WriteLine("New dep for startX: {0} startY: {1} and startX {2} start Y {3} ", hor.StartX, hor.StartY, ver.StartX, ver.StartY);
                    }
                }
            }
        }

        public int MaxWordLength
        {
            get { return Math.Max(_sizeX, _sizeY); }
        }

        public int GetPatternCount()
        {
            return _horizontalPatterns.Count + _verticalPatterns.Count;
        }

        public CrossPattern GetCrossPattern(int aIndex)
        {
            if (aIndex < _horizontalPatterns.Count)
            {
                return _horizontalPatterns[aIndex];
            }
            return _verticalPatterns[aIndex - _horizontalPatterns.Count];
        }

        public IList<CrossPattern> CrossPatterns
        {
            get
            {
                var patterns = new List<CrossPattern>();
                int cnt = this.GetPatternCount();
                for (int i = 0; i < cnt; i++)
                {
                    patterns.Add(this.GetCrossPattern(i));
                }
                return patterns;
            }
        }

        // get pattern at given position
        public CrossPattern GetCrossPattern(int aStartX, int aStartY, Orientation aOrientation)
        {
            throw new NotImplementedException();
        }

        public CrossPattern GetMostConstrainedPattern(ICrossDictionary aDict)
        {
            var min = (int)Constants.Unbounded;
            CrossPattern result = null;
            foreach (var p in _horizontalPatterns)
            {
                if (p.InstantiationCount >= min)
                {
                    continue;
                }
                result = p;
                min = p.InstantiationCount;
            }
            foreach (var p in _verticalPatterns)
            {
                if (p.InstantiationCount >= min)
                {
                    continue;
                }
                result = p;
                min = p.InstantiationCount;
            }
            return result;
        }

        public void WriteTo(StreamWriter writer)
        {
            var board = new char[_sizeX, _sizeY];

            foreach (var sw in _startWords)
            {
                board[sw.StartX, sw.StartY] = '-';
            }

            foreach (var p in _horizontalPatterns)
            {
                for (int x = p.StartX; x < p.StartX + p.Length; x++)
                {
                    if (p.Pattern != null)
                    {
                        board[x, p.StartY] = p.Pattern[x - p.StartX];
                    }
                    else
                    {
                        board[x, p.StartY] = '.';
                    }
                }
            }
            foreach (var p in _verticalPatterns)
            {
                for (int y = p.StartY; y < p.StartY + p.Length; y++)
                {
                    if (p.Pattern != null)
                    {
                        var c = p.Pattern[y - p.StartY];
                        if (c != '.')
                        {
                            board[p.StartX, y] = c;
                        }
                    }
                }
            }

            for (int y = 0; y < _sizeY; y++)
            {
                string row = "";
                for (int x = 0; x < _sizeX; x++)
                {
                    row += board[x, y] + " ";
                }
                writer.WriteLine("{0:00}: {1}", y, row);
            }

            writer.WriteLine();
        }

        public void WritePatternsTo(StreamWriter writer)
        {
            writer.WriteLine("Patterns: ");
            int cnt = GetPatternCount();
            for (int i = 0; i < cnt; i++)
            {
                writer.WriteLine(GetCrossPattern(i));
            }
        }

        public void WritePatternsTo(StreamWriter writer, ICrossDictionary dictionary)
        {
            writer.WriteLine("Patterns: ");
            int cnt = GetPatternCount();
            for (int i = 0; i < cnt; i++)
            {
                var pattern = GetCrossPattern(i);
                var word = pattern.GetWord();
                var description = GetDescription(dictionary, word);

                writer.WriteLine(string.Format("{0},{1}", pattern, description));
            }
        }

        public void WriteTemplateTo(StreamWriter writer)
        {
            for (int row = 0; row < _sizeY; row++)
            {
                for (int col = 0; col < _sizeX; col++)
                {
                    // check if we have a start word at this xy coordinate
                    if (_startWords.Any(a => a.StartX == col && a.StartY == row))
                    {
                        writer.Write("-");
                    }
                    else
                    {
                        writer.Write(" ");
                    }
                }

                if (row != _sizeY - 1) writer.WriteLine();
            }
        }

        public void CheckPatternValidity()
        {
            foreach (var p in _horizontalPatterns)
            {
                for (int i = 0; i < p.AdjacentPatterns.Length; i++)
                {
                    var ap = p.AdjacentPatterns[i];
                    if (ap == null) continue;
                    if (ap.Pattern[p.StartY - ap.StartY] != p.Pattern[i])
                    {
                        throw new Exception("X/Y inconsistency");
                    }
                }
            }

            foreach (var p in _verticalPatterns)
            {
                for (int i = 0; i < p.AdjacentPatterns.Length; i++)
                {
                    var ap = p.AdjacentPatterns[i];
                    if (ap == null) continue;
                    if (ap.Pattern[p.StartX - ap.StartX] != p.Pattern[i])
                    {
                        throw new Exception("Y/X inconsistency");
                    }
                }
            }
        }

        public ICrossBoard Clone()
        {
            var result = new CrossBoard();
            result.SetBoardSize(_sizeX, _sizeY);
            result._startWords.AddRange(_startWords);
            if (_horizontalPatterns != null && _verticalPatterns != null)
            {
                result._horizontalPatterns = new List<CrossPattern>();
                foreach (var horPattern in _horizontalPatterns)
                {
                    result._horizontalPatterns.Add((CrossPattern)horPattern.Clone());
                }

                result._verticalPatterns = new List<CrossPattern>();
                foreach (var verPattern in _verticalPatterns)
                {
                    result._verticalPatterns.Add((CrossPattern)verPattern.Clone());
                }
                result.BindAdjacentPatterns();
            }
            return result;
        }

        private static string GetDescription(ICrossDictionary dictionary, string word)
        {
            string description;
            if (!dictionary.TryGetDescription(word, out description))
            {
                description = "[PUZZLE]";
            }

            return description;
        }

        public CrossWordTimes ToCrossWordModel(ICrossDictionary dictionary)
        {
            var model = new CrossWordTimes();

            var board = new char[_sizeX, _sizeY];

            foreach (var sw in _startWords)
            {
                board[sw.StartX, sw.StartY] = '-';
            }

            var patterns = new List<CrossPattern>();
            var stringWriter = new StringWriter();

            // across = horizontal
            foreach (var p in _horizontalPatterns)
            {
                patterns.Add(p);

                for (int x = p.StartX; x < p.StartX + p.Length; x++)
                {
                    if (p.Pattern != null)
                    {
                        board[x, p.StartY] = p.Pattern[x - p.StartX];
                    }
                    else
                    {
                        board[x, p.StartY] = '.';
                    }
                }

                // stringWriter.WriteLine("{0}<br>", p);
            }

            // down = vertical
            foreach (var p in _verticalPatterns)
            {
                patterns.Add(p);

                for (int y = p.StartY; y < p.StartY + p.Length; y++)
                {
                    if (p.Pattern != null)
                    {
                        var c = p.Pattern[y - p.StartY];
                        if (c != '.')
                        {
                            board[p.StartX, y] = c;
                        }
                    }
                }

                // stringWriter.WriteLine("{0}<br>", p);
            }

            // calculate grid numbers and build answer and clues lists
            var acrossAnswerList = new List<String>();
            var downAnswerList = new List<String>();
            var acrossClueList = new List<String>();
            var downClueList = new List<String>();

            model.Gridnums = new long[_sizeX * _sizeY];
            model.Circles = new long[_sizeX * _sizeY];
            var sortedPatterns = patterns.OrderBy(s => s.StartY).ThenBy(s => s.StartX);
            int gridNumber = 0;
            CrossPattern lastPattern = null;
            var coordinateMap = new Dictionary<CrossPattern, Coordinate>();

            // when using a database - read in all descriptions once
            dictionary.AddAllDescriptions(patterns.Select(a => a.GetWord()).ToList());

            foreach (var p in sortedPatterns)
            {
                if (lastPattern != null && lastPattern.StartX == p.StartX && lastPattern.StartY == p.StartY)
                {
                    // patterns starts at same index
                }
                else
                {
                    // pattern start at new index, increment
                    gridNumber++;
                }

                // store grid number as a part of the coordinate
                coordinateMap.Add(p, new Coordinate(p.StartX, p.StartY, gridNumber));

                // and store the clues
                var word = p.GetWord();
                var description = p.IsPuzzle ? "[PUZZLE]" : GetDescription(dictionary, word);

                string clue = string.Format("{0}. {1}", gridNumber, description);
                if (p.IsHorizontal)
                {
                    acrossAnswerList.Add(word);
                    acrossClueList.Add(clue);
                }
                else
                {
                    downAnswerList.Add(word);
                    downClueList.Add(clue);
                }

                // save last pattern to compare with
                lastPattern = p;
            }

            // build output
            var grid = new List<String>();
            for (int y = 0; y < _sizeY; y++)
            {
                string row = "";
                for (int x = 0; x < _sizeX; x++)
                {
                    row += board[x, y] + " ";

                    // set grid but replace - with .
                    grid.Add(board[x, y].ToString().Replace('-', '.'));

                    if (board[x, y] != '-')
                    {
                        var coordinate = new Coordinate(x, y);
                        if (coordinateMap.ContainsValue(coordinate))
                        {
                            Coordinate foundCoordinate = null;
                            var coordinates = coordinateMap.Where(v => v.Value == coordinate);
                            if (coordinates.Count(a => a.Key.IsPuzzle) > 0)
                            {
                                var hit = coordinates.Where(a => a.Key.IsPuzzle).First();
                                var IsHorizontal = hit.Key.IsHorizontal;
                                var letterCount = hit.Key.Length;

                                // highlight all cells covered by the word
                                foundCoordinate = hit.Value;
                                if (IsHorizontal)
                                {
                                    for (int i = 0; i < letterCount; i++)
                                    {
                                        model.Circles[(y * _sizeX) + x + i] = 1;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < letterCount; i++)
                                    {
                                        model.Circles[((y * _sizeX) + x) + (i * _sizeX)] = 1;
                                    }
                                }
                            }
                            else
                            {
                                foundCoordinate = coordinates.First().Value;
                            }
                            model.Gridnums[(y * _sizeX) + x] = foundCoordinate.GridNumber;
                        }
                    }
                }

                // stringWriter.WriteLine("{0:00}: {1} <br>", y, row);
            }

            model.Title = "Generated Crossword";
            model.Author = "the amazing crossword generator";
            model.Copyright = "Crossword Generator";
            model.Size = new Size { Cols = _sizeX, Rows = _sizeY };
            // model.Notepad = "<br>" + stringWriter.ToString();
            model.Grid = grid.ToArray();
            model.Clues = new Answers() { Across = acrossClueList.ToArray(), Down = downClueList.ToArray() };
            model.Answers = new Answers() { Across = acrossAnswerList.ToArray(), Down = downAnswerList.ToArray() };
            model.Shadecircles = false;

            return model;
        }

        public CrossWordGuardian ToCrossWordModelGuardian(ICrossDictionary dictionary)
        {
            var model = new CrossWordGuardian();
            var clueList = new List<IClue>();

            var patterns = new List<CrossPattern>();

            // across = horizontal
            foreach (var p in _horizontalPatterns)
            {
                patterns.Add(p);
            }

            // down = vertical
            foreach (var p in _verticalPatterns)
            {
                patterns.Add(p);
            }

            var sortedPatterns = patterns.OrderBy(s => s.StartY).ThenBy(s => s.StartX);
            int gridNumber = 0;
            CrossPattern lastPattern = null;
            var coordinateMap = new Dictionary<CrossPattern, Coordinate>();

            // when using a database - read in all descriptions once
            dictionary.AddAllDescriptions(patterns.Select(a => a.GetWord()).ToList());

            foreach (var p in sortedPatterns)
            {
                if (lastPattern != null && lastPattern.StartX == p.StartX && lastPattern.StartY == p.StartY)
                {
                    // patterns starts at same index
                }
                else
                {
                    // pattern start at new index, increment
                    gridNumber++;
                }

                // store grid number as a part of the coordinate
                coordinateMap.Add(p, new Coordinate(p.StartX, p.StartY, gridNumber));

                // and store the clues
                var word = p.GetWord();
                var description = p.IsPuzzle ? "[PUZZLE]" : GetDescription(dictionary, word);

                var clue = new IClue();
                clue.Id = string.Format("{0}-{1}", gridNumber, p.IsHorizontal ? "across" : "down");
                clue.Number = gridNumber;
                clue.HumanNumber = gridNumber.ToString();
                clue.Clue = string.Format("{0} ({1})", description, p.Length);
                clue.Direction = p.IsHorizontal ? Direction.Across : Direction.Down;
                clue.Length = p.Length;
                clue.Group = new string[] { clue.Id };
                clue.Position = new IPosition() { X = p.StartX, Y = p.StartY };
                clue.Solution = word;
                clue.SeparatorLocations = new SeparatorLocations();

                clueList.Add(clue);

                // save last pattern to compare with
                lastPattern = p;
            }

            model.Id = null;
            model.Name = "Generated Crossword";
            model.Creator = new ICreator() { Name = "the amazing crossword generator", WebUrl = "" };
            model.Entries = clueList.ToArray();
            model.Dimensions = new IDimensions { Cols = _sizeX, Rows = _sizeY };
            model.CrosswordType = CrosswordType.Quick;
            model.SolutionAvailable = true;
            model.Pdf = null;
            model.Instructions = null;
            model.Date = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            model.DateSolutionAvailable = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return model;
        }
    }
}