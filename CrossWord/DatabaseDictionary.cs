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
        readonly bool _doSQLDebug = false;

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
                // search for all words
                // var words = db.Words
                //     .Where((w => (w.NumberOfWords == 1) && (w.NumberOfLetters <= maxWordLength)))
                //     .OrderBy(w => w.Value)
                //     .Select(w => w.Value);

                // in order to sort with Collation we need to use raw SQL
                var words = db.Words.FromSql(
                    $"SELECT * FROM Words AS w WHERE w.NumberOfWords = 1 AND w.NumberOfLetters <= {_maxWordLength} ORDER BY w.Value COLLATE utf8mb4_da_0900_as_cs")
                    .Select(w => w.Value)
                    .AsNoTracking();

                foreach (var word in words)
                {
                    string wordText = word;
                    if (wordText.All(Char.IsLetter))
                    {
                        AddWord(wordText);
                    }
                }
            }
        }

        public int MaxWordLength
        {
            get { return _maxWordLength; }
        }

        public void AddAllDescriptions(List<string> words)
        {
            Random rnd = new Random();

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
                    .Where(x => newWords.Contains(x.Value))
                    .AsNoTracking();

                foreach (var word in existingWords)
                {
                    var wordText = word.Value;

                    if (word.RelatedFrom.Count > 0)
                    {
                        // string hintText = word.RelatedFrom.Last().WordTo.Value

                        // randomize the word descriptions
                        int indexFrom = rnd.Next(0, word.RelatedFrom.Count - 1);
                        string hintText = word.RelatedFrom.ToArray()[indexFrom].WordTo.Value;

                        AddDescription(wordText, hintText);
                    }
                    else if (word.RelatedTo.Count > 0)
                    {
                        // string hintText = word.RelatedTo.Last().WordFrom.Value;

                        // randomize the word descriptions
                        int indexTo = rnd.Next(0, word.RelatedTo.Count - 1);
                        string hintText = word.RelatedTo.ToArray()[indexTo].WordFrom.Value;

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