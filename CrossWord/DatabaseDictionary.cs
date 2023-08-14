using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CrossWord.Scraper.MySQLDbService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CrossWord;

public class DatabaseDictionary : ICrossDictionary
{
    WordFilter _filter;
    IList<string>[] _words; // different array list for each word length
    WordIndex[] _indexes;
    Dictionary<string, string> _description;
    int _maxWordLength; // longest possible word in number of characters

    readonly bool _doSQLDebug = false;
    readonly string? _connectionString;

    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly Microsoft.Extensions.Logging.ILogger? _logger;

    private WordHintDbContext CreateDbContext()
    {
        _logger?.LogDebug("CreateDbContext()");

        if (_scopeFactory != null)
        {
            _logger?.LogDebug("Getting WordHintDbContext using scopeFactory.");
            var scope = _scopeFactory.CreateScope();
            return scope.ServiceProvider.GetRequiredService<WordHintDbContext>();
        }
        else
        {
            if (_doSQLDebug)
            {
                _logger?.LogDebug("Getting WordHintDbContext using DesignTimeDbContextFactory (SQL debugging).");
                var dbContextFactory = new DesignTimeDbContextFactory();
                return dbContextFactory.CreateDbContext(_connectionString, Log.Logger);
            }
            else
            {
                _logger?.LogDebug("Getting WordHintDbContext using DbContextOptionsBuilder.");
                var options = new DbContextOptionsBuilder<WordHintDbContext>();
                options.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));
                return new WordHintDbContext(options.Options);
            }
        }
    }

    public IEnumerable<string>[] Words
    {
        get { return _words; }
    }

    public IDictionary<string, string> Descriptions
    {
        get { return _description; }
    }

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

    public DatabaseDictionary(string connectionString, int maxWordLength, ILoggerFactory loggerFactory, bool doSQLDebug = false)
        : this(maxWordLength)
    {
        _connectionString = connectionString;    
        _doSQLDebug = doSQLDebug;    

        _logger = loggerFactory.CreateLogger<DatabaseDictionary>();
        _logger?.LogInformation("Initializing Database Dictionary");

        using var db = CreateDbContext();

        // setup database
        // You would either call EnsureCreated() or Migrate(). 
        // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
        // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
        // db.Database.EnsureDeleted();
        // db.Database.EnsureCreated();

        // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
        db.Database.Migrate();

        ReadWordsIntoDatabase(db);
    }

    // This is intitialized from the API using services.AddSingleton<ICrossDictionary, DatabaseDictionary>();
    public DatabaseDictionary(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, int maxWordLength = 25)
    : this(maxWordLength)
    {
        _scopeFactory = scopeFactory;

        _logger = loggerFactory.CreateLogger("DatabaseDictionary");
        _logger?.LogInformation("Initializing Database Dictionary");

        ResetDictionary(maxWordLength);
    }

    public void ResetDictionary(int maxWordLength = 25)
    {
        using var db = CreateDbContext();
        ReadWordsIntoDatabase(db);

        // when we exit the using block,
        // the IServiceScope will dispose itself 
        // and dispose all of the services that it resolved.
    }

    private void ReadWordsIntoDatabase(WordHintDbContext db)
    {
        _logger?.LogInformation("Starting to read words into database...");

#if DEBUG
        // Create new stopwatch.
        Stopwatch stopwatch = new();

        // Begin timing.
        stopwatch.Start();
#endif

        // var wordIdsToExclude = WordDatabaseService.GetWordIdList(db, new List<string> { "BY", "NAVN", "ELV", "FJELL", "FORKORTELSE", "IATA-FLYPLASSKODE", "ISO-KODE" });
        // var wordIdsToExclude = WordDatabaseService.GetWordIdList(db, new List<string> { "BY", "NAVN" });

        // search for all words excluding an array of ids
        // var words = db.Words
        //     .Where(w => (w.NumberOfWords == 1) && (w.NumberOfLetters <= _maxWordLength) && !wordIdsToExclude.Contains(w.WordId))
        //     .OrderBy(w => w.Value)
        //     .Select(w => w.Value)
        //     .AsNoTracking();

        // search for all words
        // var words = db.Words
        //     .Where(w => (w.NumberOfWords == 1) && (w.NumberOfLetters <= _maxWordLength))
        //     .OrderBy(w => w.Value)
        //     .Select(w => w.Value)
        //     .AsNoTracking();

        // in order to sort with Collation we need to use raw SQL
        // var words = db.Words.FromSql(
        //     $"SELECT w.Value FROM Words AS w WHERE w.NumberOfWords = 1 AND w.NumberOfLetters <= {_maxWordLength} ORDER BY w.Value COLLATE utf8mb4_da_0900_as_cs")
        //     .Select(w => w.Value)
        //     .AsNoTracking();

        // foreach (var word in words)
        // {
        //     string wordText = word;
        //     if (wordText.All(char.IsLetter))
        //     // if (wordText.All(x => char.IsLetter(x) || x == '-' || x == ' '))
        //     {
        //         AddWord(wordText);
        //     }
        // }

        // using ADO.NET seems faster than ef core for raw SQLs
        using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            // limit to only words that are only uppercase A-Å
            command.CommandText =
            $@"SELECT w.Value 
            FROM Words AS w 
            WHERE w.NumberOfWords = 1 
            AND w.NumberOfLetters <= {_maxWordLength}
            AND w.Value REGEXP '^[A-Å]+$'
            ORDER BY w.Value COLLATE utf8mb4_da_0900_as_cs;";

            if (_doSQLDebug) _logger?.LogDebug(command.CommandText);

            db.Database.OpenConnection();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string? wordText = reader[0].ToString();
                    AddWord(wordText);
                }
            }
        }

#if DEBUG
        // Stop timing.
        stopwatch.Stop();

        // Write result.
        _logger?.LogInformation("Succesfully read words into database - Time elapsed: {0}", stopwatch.Elapsed);
#else
    _logger?.LogInformation("Succesfully read words into database!");
#endif

    }

    private void ReadDescriptionsIntoDatabase(WordHintDbContext db, List<string> words)
    {
        _logger?.LogInformation("Starting to read descriptions into database...");

#if DEBUG
        // Create new stopwatch.
        Stopwatch stopwatch = new();

        // Begin timing.
        stopwatch.Start();
#endif

        // increase timeout
        db.Database.SetCommandTimeout(400);

        // find out which words have already a description
        var newWords = words.Where(value => !_description.Any(entry => entry.Key == value));
        if (newWords.Any())
        {
            // find the words in the database
            // https://nishanc.medium.com/writing-better-performant-queries-with-linq-on-ef-core-6-0-%EF%B8%8F-85a1a406879
            // var existingWords = db.Words
            //    .Include(w => w.RelatedFrom)
            //    .ThenInclude(w => w.WordTo)
            //    .Include(w => w.RelatedTo)
            //    .ThenInclude(w => w.WordFrom)
            // // .AsSplitQuery() // use split query to avoid timeouts
            //    .AsSingleQuery()
            //    .Where(x => newWords.Contains(x.Value))
            //    .AsNoTracking();

            // Random rnd = new();

            // foreach (var word in existingWords)
            // {
            //     var wordText = word.Value;

            //     if (word.RelatedFrom.Count > 0)
            //     {
            //         // string hintText = word.RelatedFrom.Last().WordTo.Value

            //         // randomize the word descriptions
            //         int indexFrom = rnd.Next(0, word.RelatedFrom.Count - 1);
            //         string hintText = word.RelatedFrom.ToArray()[indexFrom].WordTo.Value;

            //         AddDescription(wordText, hintText);
            //     }
            //     else if (word.RelatedTo.Count > 0)
            //     {
            //         // string hintText = word.RelatedTo.Last().WordFrom.Value;

            //         // randomize the word descriptions
            //         int indexTo = rnd.Next(0, word.RelatedTo.Count - 1);
            //         string hintText = word.RelatedTo.ToArray()[indexTo].WordFrom.Value;

            //         AddDescription(wordText, hintText);
            //     }
            // }

            // using ADO.NET seems faster than ef core for raw SQLs
            using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                var inClause = string.Join(", ", newWords.Select(word => $"'{word}'"));
                int maxHintsPerWord = 10;
                command.CommandText =
                    $@"SELECT sub.Value, sub.RelatedValue
                    FROM (
                        SELECT
                            w1.WordId,
                            w1.Value,
                            w2.Value AS RelatedValue,
                            ROW_NUMBER() OVER (PARTITION BY w1.WordId ORDER BY w2.WordId) AS row_num
                        FROM Words AS w1
                        LEFT JOIN WordRelations AS wr1 ON w1.WordId = wr1.WordFromId
                        LEFT JOIN Words AS w2 ON wr1.WordToId = w2.WordId
                        WHERE w1.Value IN ({inClause})
                    ) AS sub
                    WHERE sub.row_num <= {maxHintsPerWord};";

                if (_doSQLDebug) _logger?.LogDebug(command.CommandText);

                db.Database.OpenConnection();
                using (var reader = command.ExecuteReader())
                {
                    // Random number generator for random hintText selection
                    var random = new Random();

                    while (reader.Read())
                    {
                        string? wordText = reader[0].ToString();   // Get wordText from the reader
                        string? hintText = reader[1].ToString();   // Get hintText from the reader

                        if (!Descriptions.ContainsKey(wordText))
                        {
                            // If wordId is encountered for the first time, add hintText to description map
                            AddDescription(wordText!, hintText!);
                        }
                        else
                        {
                            // If wordId has been encountered before, randomly decide whether to update hintText
                            if (random.Next(2) == 0)
                            {
                                AddDescription(wordText!, hintText!); // Call AddDescription with updated hintText
                            }
                        }
                    }
                }
            }
        }

#if DEBUG
        // Stop timing.
        stopwatch.Stop();

        // Write result.
        _logger?.LogInformation("Succesfully read descriptions into database - Time elapsed: {0}", stopwatch.Elapsed);
#else
    _logger?.LogInformation("Succesfully read descriptions into database!");
#endif

    }

    public void AddAllDescriptions(List<string> words)
    {
        using var db = CreateDbContext();
        ReadDescriptionsIntoDatabase(db, words);
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

    // Implementations using a database - too slow to be used directly
    private string? GetDescriptionDatabase(string wordText)
    {
        using var db = CreateDbContext();
        string? description = null;

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

    public void GetMatchDatabase(ReadOnlySpan<char> pattern, List<string> matched)
    {
        using var db = CreateDbContext();
        var patternSQL = new string(pattern).Replace('.', '_'); // underscore is the any character in SQL

        var words = db.Words
            .Where(c => EF.Functions.Like(c.Value, patternSQL))
            .OrderByDescending(p => p.WordId)
            .Select(a => a.Value)
            .ToList();

        words.RemoveAll(s => s.Any(c => !char.IsLetter(c)));

        matched.AddRange(words);
        return;
    }

    public int GetMatchCountDatabase(ReadOnlySpan<char> pattern)
    {
        using var db = CreateDbContext();
        var patternSQL = new string(pattern).Replace('.', '_'); // underscore is the any character in SQL

        // var wordsCount = db.Words
        // .Count(c => EF.Functions.Like(c.Value, patternSQL));

        var words = db.Words
            .Where(c => EF.Functions.Like(c.Value, patternSQL))
            .OrderByDescending(p => p.WordId)
            .Select(a => a.Value)
            .ToList();

        words.RemoveAll(s => s.Any(c => !char.IsLetter(c)));

        var wordsCount = words.Count;

        return wordsCount;
    }

    public int GetWordOfLengthCountDatabase(int length)
    {
        //  how many words do we have with each letter length?
        using var db = CreateDbContext();
        var patternSQL = new string('_', length); // underscore is the any character in SQL

        var wordsCount = db.Words
        .Count(c => EF.Functions.Like(c.Value, patternSQL));

        return wordsCount;
    }
}
