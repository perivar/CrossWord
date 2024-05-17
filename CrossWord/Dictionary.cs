using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace CrossWord;

public class Dictionary : ICrossDictionary
{
	readonly WordFilter _filter;
	readonly List<string>[] _words; //different array list for each word length
	readonly WordIndex[] _indexes;
	readonly Dictionary<string, string> _description;

	public IEnumerable<string>[] Words
	{
		get { return _words; }
	}

	public IDictionary<string, string> Descriptions
	{
		get { return _description; }
	}

	public Dictionary(int maxWordLength)
	{
		_words = new List<string>[maxWordLength + 1];
		for (int i = 1; i <= maxWordLength; i++)
		{
			_words[i] = new List<string>();
		}

		_indexes = new WordIndex[maxWordLength + 1];
		for (int i = 1; i <= maxWordLength; i++)
		{
			_indexes[i] = new WordIndex(i);
		}

		_filter = new WordFilter(1, maxWordLength);
		_description = new Dictionary<string, string>();
	}

	public Dictionary(string dictionaryFile, int maxWordLength)
		: this(maxWordLength)
	{
		if (Path.GetExtension(dictionaryFile).ToLower().Equals(".json"))
		{
			// read json files
			using StreamReader r = new(dictionaryFile);
			var json = r.ReadToEnd();
			var jobj = JObject.Parse(json);
			foreach (var item in jobj.Properties())
			{
				var description = item.Name;
				var values = item.Values();
				foreach (var value in values)
				{
					var word = value.Value<string>().ToUpper();
					AddWord(word);
					AddDescription(word, description);
				}
			}
		}
		else if (Path.GetExtension(dictionaryFile).ToLower().Equals(".dat"))
		{
			// read first line from the file using default encoding
			Encoding? encoding = null;
			using (StreamReader reader = new(dictionaryFile))
			{
				var encodingString = reader.ReadLine();
				if (!string.IsNullOrEmpty(encodingString))
				{
					encoding = Encoding.GetEncoding(encodingString);
				}
			}

			if (encoding == null)
			{
				throw new Exception("Could not detect file encoding!");
			}

			using (StreamReader reader = new(dictionaryFile, encoding))
			{
				string linePattern = @"\|(\d+)$"; // Regex pattern to detect lines ending with |<number>
				int lineCounter = 0;
				while (!reader.EndOfStream)
				{
					string? line = reader.ReadLine();
					lineCounter++;

					// skip first line
					if (lineCounter == 1) continue;

					string description = "";
					int numberOfLinesToRead = 0;
					if (!string.IsNullOrEmpty(line))
					{
						Match match = Regex.Match(line, linePattern);
						if (match.Success)
						{
							description = line.Substring(0, match.Index);
							numberOfLinesToRead = int.Parse(match.Groups[1].Value);

							var relatedArray = new HashSet<string>();
							for (int i = 0; i < numberOfLinesToRead; i++)
							{
								string? l = reader.ReadLine();
								if (!string.IsNullOrEmpty(l))
								{
									var synonyms = l.Split('|').Skip(1); // ignore the first "-" entry
									foreach (string synonym in synonyms)
									{
										relatedArray.Add(synonym);
									}
								}
							}

							// Define a regex pattern to match "(word)"
							string pattern = @"^\([^)]+\)\s*";
							description = Regex.Replace(description, pattern, ""); // Replace the pattern with an empty string

							// Console.WriteLine("{0} {1}:{2}", lineCounter, word, string.Join(",", synonyms));

							foreach (string synonym in relatedArray)
							{
								var word = synonym.ToUpper();
								AddWord(word);
								AddDescription(word, description);
							}
						}
					}
				}
			}
		}
		else
		{
			// read streams
			using var reader = File.OpenText(dictionaryFile);
			var str = reader.ReadLine();
			var ti = new CultureInfo("en-US").TextInfo;
			while (str != null)
			{
				int pos = str.IndexOf('|');
				if (pos == -1)
				{
					AddWord(ti.ToUpper(str));
				}
				else
				{
					var word = str.Substring(0, pos);
					AddWord(word);
					AddDescription(word, str.Substring(pos + 1));
				}

				str = reader.ReadLine();
			}
		}
	}

	public void AddDescription(string word, string description)
	{
		_description[word] = description;
	}

	public bool TryGetDescription(string word, out string? description)
	{
		return _description.TryGetValue(word, out description);
	}

	public void AddWord(string word)
	{
		if (!_filter.Filter(word)) return;
		_indexes[word.Length].IndexWord(word, _words[word.Length].Count);
		_words[word.Length].Add(word);
	}

	public int GetWordOfLengthCount(int length)
	{
		return _words[length].Count;
	}

	static bool IsEmptyPattern(ReadOnlySpan<char> pattern)
	{
		for (var i = 0; i < pattern.Length; i++)
			if (pattern[i] != '.')
				return false;
		return true;
	}

	public int GetMatchCount(ReadOnlySpan<char> pattern)
	{
		if (IsEmptyPattern(pattern))
			return _words[pattern.Length].Count;
		return _indexes[pattern.Length].GetMatchingIndexCount(pattern);
	}

	public void GetMatch(ReadOnlySpan<char> pattern, List<string> matched)
	{
		if (IsEmptyPattern(pattern))
		{
			matched.AddRange(_words[pattern.Length]);
			return;
		}

		var indexes = _indexes[pattern.Length].AddMatched(pattern);
		foreach (var idx in indexes)
		{
			matched.Add(_words[pattern.Length][idx]);
		}
	}

	public void AddAllDescriptions(List<string> words)
	{
		// this can safely be ignored since all the descriptions has already been loaded at load time
	}

	public void ResetDictionary(int maxWordLength)
	{
		// this can safely be ignored since all this has been loaded at load time
	}
}