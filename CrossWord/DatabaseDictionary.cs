using System;
using System.Collections.Generic;
using System.Linq;
using CrossWord.Scraper.MySQLDbService;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CrossWord
{
    public class DatabaseDictionary : ICrossDictionary
    {
        static WordHintDbContext CreateDbContext(string dbConnectionString, bool doDebug = false)
        {
            if (doDebug)
            {
                var dbContextFactory = new DesignTimeDbContextFactory();
                return dbContextFactory.CreateDbContext(dbConnectionString, null);
            }
            else
            {
                var options = new DbContextOptionsBuilder<WordHintDbContext>();
                options.UseMySql(dbConnectionString);
                return new WordHintDbContext(options.Options);
            }
        }

        string ConnectionString { get; set; }
        readonly bool _doSQLDebug;

        readonly WordFilter _filter;
        readonly IList<string>[] _words; // different array list for each word length
        readonly WordIndex[] _indexes;
        readonly int _maxWordLength; // longest possible word in number of characters
        readonly Dictionary<string, string> _description;

        public IList<string>[] Words { get { return _words; } }

        public Dictionary<string, string> Description { get { return _description; } }

        public DatabaseDictionary(int maxWordLength)
        {
            _maxWordLength = maxWordLength;
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

        public DatabaseDictionary(string connectionString, int maxWordLength)
            : this(maxWordLength)
        {
            this.ConnectionString = connectionString;
            // this._doSQLDebug = true;

            using (var db = CreateDbContext(ConnectionString, _doSQLDebug))
            {
                // var words = db.WordRelations
                //     .Include(w => w.WordFrom)
                //     .Include(w => w.WordTo)
                //     .Where((w => w.WordFrom.NumberOfLetters <= maxWordLength))
                //     .GroupBy(wr => wr.WordFromId)
                //     .Select(w => new { WordFromId = w.Key, WordRelation = w.First() })
                // // ;
                // .Take(200000);

                // foreach (var word in words)
                // {
                //     string wordText = word.WordRelation.WordFrom.Value;
                //     string hintText = word.WordRelation.WordTo.Value;

                //     if (wordText.All(Char.IsLetter))
                //     {
                //         AddWord(wordText);
                //         AddDescription(wordText, hintText);
                //     }
                // }

                // search for all words
                var words = db.Words
                    .Where((w => w.NumberOfLetters <= maxWordLength))
                    .Select(w => w.Value);

                foreach (var word in words)
                {
                    string wordText = word;
                    if (wordText.All(Char.IsLetter))
                    {
                        AddWord(wordText);
                    }
                }

                // for (int wordLength = 1; wordLength <= maxWordLength; wordLength++)
                // {
                //     // search for letters for each length
                //     var words = db.Words
                //         // .Include(w => w.RelatedFrom)
                //         // .ThenInclude(w => w.WordTo)
                //         // .Include(w => w.RelatedTo)
                //         // .ThenInclude(w => w.WordFrom)
                //         .Where((w => w.NumberOfLetters == wordLength))
                //         // .OrderByDescending(w => w.CreatedDate)
                //         // .Take(5000);
                //         ;

                //     foreach (var word in words)
                //     {
                //         string wordText = word.Value;
                //         if (wordText.All(Char.IsLetter))
                //         {
                //             // if (word.RelatedFrom.Count > 0)
                //             // {
                //             //     string hintText = word.RelatedFrom.Last().WordTo.Value;
                //             //     AddWord(wordText);
                //             //     AddDescription(wordText, hintText);
                //             // }
                //             // else if (word.RelatedTo.Count > 0)
                //             // {
                //             //     string hintText = word.RelatedTo.Last().WordFrom.Value;
                //             //     AddWord(wordText);
                //             //     AddDescription(wordText, hintText);
                //             // }
                //         }
                //     }
                // }
            }
        }

        public int MaxWordLength
        {
            get { return _maxWordLength; }
        }

        public void AddAllDescriptions(List<string> words)
        {
            using (var db = CreateDbContext(ConnectionString, _doSQLDebug))
            {
                // find out which words have already a description
                var newWords = words.Where(value => !_description.Any(entry => entry.Key == value));

                // find the words in the database
                var existingWords = db.Words
                    .Include(w => w.RelatedFrom)
                    .ThenInclude(w => w.WordTo)
                    .Include(w => w.RelatedTo)
                    .ThenInclude(w => w.WordFrom)
                    .Where(x => newWords.Contains(x.Value));

                foreach (var word in existingWords)
                {
                    var wordText = word.Value;

                    if (word.RelatedFrom.Count > 0)
                    {
                        string hintText = word.RelatedFrom.Last().WordTo.Value;
                        AddDescription(wordText, hintText);
                    }
                    else if (word.RelatedTo.Count > 0)
                    {
                        string hintText = word.RelatedTo.Last().WordFrom.Value;
                        AddDescription(wordText, hintText);
                    }
                }
            }
        }

        public void AddDescription(string word, string description)
        {
            _description[word] = description;
        }

        public bool TryGetDescription(string word, out string description)
        {
            return _description.TryGetValue(word, out description);
        }

        public void AddWord(string aWord)
        {
            if (!_filter.Filter(aWord)) return;
            _indexes[aWord.Length].IndexWord(aWord, _words[aWord.Length].Count);
            _words[aWord.Length].Add(aWord);
        }

        public int GetWordOfLengthCount(int aLength)
        {
            return _words[aLength].Count;
        }

        static bool IsEmptyPattern(char[] aPattern)
        {
            if (aPattern == null) return true;
            foreach (var c in aPattern)
            {
                if (c != '.') return false;
            }
            return true;
        }

        public int GetMatchCount(char[] aPattern)
        {
            if (IsEmptyPattern(aPattern))
                return _words[aPattern.Length].Count;
            var indexes = _indexes[aPattern.Length].GetMatchingIndexes(aPattern);
            return indexes != null ? indexes.Count : 0;
        }

        public void GetMatch(char[] aPattern, List<string> matched)
        {
            if (IsEmptyPattern(aPattern))
            {
                matched.AddRange(_words[aPattern.Length]);
                return;
            }
            var indexes = _indexes[aPattern.Length].GetMatchingIndexes(aPattern);
            if (indexes == null) return;
            foreach (var idx in indexes)
            {
                matched.Add(_words[aPattern.Length][idx]);
            }
        }

        // Implementations using a database - too slow to be used directly
        private string GetDescriptionDatabase(string wordText)
        {
            using (var db = CreateDbContext(ConnectionString, _doSQLDebug))
            {
                string description = null;

                var words = db.WordRelations
                    .Include(w => w.WordFrom)
                    .Include(w => w.WordTo)
                    .Where(w => (w.WordFrom.Value == wordText) || (w.WordTo.Value == wordText))
                    .Select(w => new { WordFrom = w.WordFrom.Value, WordTo = w.WordTo.Value });

                var word = words.FirstOrDefault();
                if (word != null)
                {
                    if (word.WordFrom == wordText)
                    {
                        description = word.WordTo;
                    }
                    else if (word.WordTo == wordText)
                    {
                        description = word.WordFrom;
                    }
                }

                return description;
            }
        }

        public void GetMatchDatabase(char[] pattern, List<string> matched)
        {
            using (var db = CreateDbContext(ConnectionString, _doSQLDebug))
            {
                var patternSQL = new string(pattern).Replace('.', '_'); // underscore is the any character in SQL

                var words = db.Words
                    .Where(c => EF.Functions.Like(c.Value, patternSQL))
                    .OrderByDescending(p => p.WordId)
                    .Select(a => a.Value)
                    .ToList();

                words.RemoveAll(s => s.Any(c => !Char.IsLetter(c)));

                matched.AddRange(words);
                return;
            }
        }

        public int GetMatchCountDatabase(char[] pattern)
        {
            using (var db = CreateDbContext(ConnectionString, _doSQLDebug))
            {
                var patternSQL = new string(pattern).Replace('.', '_'); // underscore is the any character in SQL

                // var wordsCount = db.Words
                // .Count(c => EF.Functions.Like(c.Value, patternSQL));

                var words = db.Words
                    .Where(c => EF.Functions.Like(c.Value, patternSQL))
                    .OrderByDescending(p => p.WordId)
                    .Select(a => a.Value)
                    .ToList();

                words.RemoveAll(s => s.Any(c => !Char.IsLetter(c)));

                var wordsCount = words.Count;

                return wordsCount;
            }
        }

        public int GetWordOfLengthCountDatabase(int length)
        {
            //  how many words do we have with each letter length?
            using (var db = CreateDbContext(ConnectionString, _doSQLDebug))
            {
                var patternSQL = new string('_', length); // underscore is the any character in SQL

                var wordsCount = db.Words
                .Count(c => EF.Functions.Like(c.Value, patternSQL));

                return wordsCount;
            }
        }
    }
}