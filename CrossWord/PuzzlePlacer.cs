using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrossWord;

class PuzzlePlacer
{
	readonly string _puzzle;
	readonly ICrossBoard _board;

	public PuzzlePlacer(ICrossBoard board, string puzzle)
	{
		_board = board;
		_puzzle = puzzle;
	}

	public IEnumerable<ICrossBoard> GetAllPossiblePlacements(ICrossDictionary dictionary)
	{
		var puzzle = NormalizePuzzle(_puzzle).AsMemory();
		var board = _board.Clone();
		board.Preprocess(dictionary);

		var patterns = new List<CrossPattern>();
		for (int i = 0; i < board.GetPatternCount(); i++)
		{
			patterns.Add(board.GetCrossPattern(i));
		}

		// sort by word length
		patterns.Sort((a, b) => -1 * a.Length.CompareTo(b.Length));
		if (patterns.Count == 0)
			yield break;

		var restPuzzleLength = puzzle.Length;
		var stack = new Stack<int>();
		var appliedTransformations = new Stack<CrossTransformation>();
		int idx = 0;
		while (true)
		{
		continueOuterLoop:
			for (; idx < patterns.Count; idx++)
			{
				var pattern = patterns[idx];

				if (restPuzzleLength < pattern.Length)
				{
					continue;
				}

				if (restPuzzleLength - pattern.Length == 1)
				{
					break; // PIN: this was a continue statement - which seems like a bug
				}

				var trans = pattern.TryFillPuzzle(puzzle.Slice(puzzle.Length - restPuzzleLength,
					pattern.Length).Span, dictionary);

				if (trans != null)
				{
					trans.Transform(pattern);
					if (restPuzzleLength == pattern.Length)
					{
						// ensure only one pattern is marked as a puzzle
						patterns.All(c => { c.IsPuzzle = false; return true; });

						// set the current pattern as puzzle
						pattern.IsPuzzle = true;

						var cloned = board.Clone();  // clone before we revert the puzzle pattern
						trans.Undo(pattern);

						yield return cloned;
						continue;
					}

					stack.Push(idx + 1);
					trans.Pattern = pattern;
					appliedTransformations.Push(trans);
					restPuzzleLength -= pattern.Length;
					idx = 0;
					goto continueOuterLoop;
				}
			}

			if (stack.Count == 0)
			{
				break;
			}

			idx = stack.Pop();
			var appTr = appliedTransformations.Pop();
			appTr.Undo(appTr.Pattern);
			restPuzzleLength += appTr.Pattern.Length;
		}
	}

	static string NormalizePuzzle(string puzzle)
	{
		var builder = new StringBuilder(puzzle.Length);
		foreach (var c in puzzle)
		{
			if (char.IsLetter(c))
			{
				builder.Append(char.ToUpper(c));
			}
		}

		return builder.ToString();
	}
}