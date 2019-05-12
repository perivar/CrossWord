using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CrossWord.Scraper.MySQLDbService
{
    public static class WordDatabaseService
    {
        public static string GetLastWordFromLetterCount(WordHintDbContext db, string source, int letterCount)
        {
            if (letterCount > 0)
            {
                Log.Information("Looking for last word using letter count '{0}' from '{1}'", letterCount, source);

                var lastWordWithPatternLength = db.States
                        // .AsNoTracking()
                        .FirstOrDefault(item => item.Source == source && item.NumberOfLetters == letterCount);

                if (lastWordWithPatternLength != null)
                {
                    // detach in order to clean this from the db tracked cache
                    db.Entry(lastWordWithPatternLength).State = EntityState.Detached;

                    Log.Information("Using the last word with letter count '{0}', last word '{1}'", letterCount, lastWordWithPatternLength);
                    return lastWordWithPatternLength.Word.RemoveDiacriticsToNorwegian();
                }
            }

            return null;
        }

        public static string GetLastWordFromComment(WordHintDbContext db, string source, string comment)
        {
            if (comment != null)
            {
                Log.Information("Looking for last word using comment '{0}' from '{1}'", comment, source);

                var lastWordWithComment = db.States
                        // .AsNoTracking()
                        .FirstOrDefault(item => item.Source == source && item.Comment == comment);

                if (lastWordWithComment != null)
                {
                    // detach in order to clean this from the db tracked cache
                    db.Entry(lastWordWithComment).State = EntityState.Detached;

                    Log.Information("Using the last word with comment '{0}', last word '{1}'", comment, lastWordWithComment);
                    return lastWordWithComment.Word.RemoveDiacriticsToNorwegian();
                }
            }

            return null;
        }

        public static void AddToDatabase(WordHintDbContext db, string source, User user, string wordText, IEnumerable<string> relatedValues)
        {
            // Note that  tracking should be disabled to speed things up
            // note that this doesn't load the virtual properties, but loads the object ids after a save
            // db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            // ensure uppercase
            wordText = wordText.ToUpper();

            var word = new Word
            {
                Language = "no",
                Value = wordText,
                NumberOfLetters = ScraperUtils.CountNumberOfLetters(wordText),
                NumberOfWords = ScraperUtils.CountNumberOfWords(wordText),
                User = user,
                CreatedDate = DateTime.Now
            };

            // ensure related are all uppercase and distinct
            var relatedValuesUpperCase = relatedValues.Select(x => x.ToUpper()).Distinct();

            // get all related words (hints) as Word objects
            var relatedWords = relatedValuesUpperCase.Select(hintText => new Word
            {
                Language = "no",
                Value = hintText,
                NumberOfLetters = ScraperUtils.CountNumberOfLetters(hintText),
                NumberOfWords = ScraperUtils.CountNumberOfWords(hintText),
                User = user,
                CreatedDate = DateTime.Now
            });

            AddToDatabase(db, source, word, relatedWords);
        }

        public static void AddToDatabase(WordHintDbContext db, string source, Word word, IEnumerable<Word> relatedWords, TextWriter writer = null, bool doStoreState = true)
        {
            // Note that  tracking should be disabled to speed things up
            // note that this doesn't load the virtual properties, but loads the object ids after a save
            // db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            if (word == null || word != null && word.Value == null) return;
            if (relatedWords == null || relatedWords != null && relatedWords.Count() == 0) return;

            // check if word already exists
            var existingWord = db.Words.Where(w => w.Value == word.Value).FirstOrDefault();
            if (existingWord != null)
            {
                // update reference to existing word (i.e. reuse the word)
                word = existingWord;
            }
            else
            {
                // add new word
                db.Words.Add(word);
                db.SaveChanges();
            }

            // update that we are processing this word
            if (doStoreState)
            {
                UpdateState(db, source, word, writer);
            }

            // Note! ensure related are all uppercase before this method is called
            var relatedValuesUpperCase = relatedWords.Select(x => x.Value);

            // find out which related words already exist in the database
            var existingRelatedWords = db.Words.Where(x => relatedValuesUpperCase.Contains(x.Value)).ToList();

            // which words need to be added?
            var newRelatedWords = relatedWords.Where(x => !existingRelatedWords.Any(a => a.Value == x.Value)).ToList();

            if (newRelatedWords.Count > 0)
            {
                db.Words.AddRange(newRelatedWords);
                db.SaveChanges();

                if (writer != null) writer.WriteLine("Added '{0}'", string.Join(",", newRelatedWords.Select(i => i.Value).ToArray()));
            }
            else
            {
                if (writer != null) writer.WriteLine("Skipped adding '{0}'", string.Join(",", existingRelatedWords.Select(i => i.Value).ToArray()));
            }

            // what relations needs to be added?
            var allRelatedWords = existingRelatedWords.Concat(newRelatedWords);
            var allWordRelationsFrom = allRelatedWords.Select(hint =>
                // only use the ids to speed things up
                new WordRelation { WordFromId = word.WordId, WordToId = hint.WordId }
            );

            // add relation from each hint to word as well
            var allWordRelationsTo = allRelatedWords.Select(hint =>
                // only use the ids to speed things up
                new WordRelation { WordFromId = hint.WordId, WordToId = word.WordId }
            );

            // all relations
            var allWordRelations = allWordRelationsFrom.Concat(allWordRelationsTo).Distinct();

            // find out which relations already exist in the database
            // check both directions in the Many-to-Many Relationship 
            var allRelatedWordsIds = allRelatedWords.Select(a => a.WordId).ToList();
            var existingWordRelations = db.WordRelations.Where(a =>
                (a.WordFromId == word.WordId && allRelatedWordsIds.Contains(a.WordToId))
                ||
                (a.WordToId == word.WordId && allRelatedWordsIds.Contains(a.WordFromId))
            ).ToList();

            // which relations need to be added?
            // check both directions in the Many-to-Many Relationship             
            var newWordRelations = allWordRelations.Where(wr => !existingWordRelations.Any
            (a =>
                (a.WordFromId == wr.WordFromId && a.WordToId == wr.WordToId)
            // ||
            // (a.WordFromId == wr.WordToId && a.WordToId == wr.WordFromId)
            )).ToList();

            if (newWordRelations.Count > 0)
            {
                db.WordRelations.AddRange(newWordRelations);
                db.SaveChanges();

                if (db.ChangeTracker.QueryTrackingBehavior == QueryTrackingBehavior.NoTracking)
                {
                    // without tracking we don't have the word value, so use only the wordids
                    if (writer != null) writer.WriteLine("Related '{0}' to '{1}'", string.Join(",", newWordRelations.Select(i => i.WordToId).ToArray().Distinct()), word.Value);
                }
                else
                {
                    // with tracking we can output the actual words
                    if (writer != null) writer.WriteLine("Related '{0}' to '{1}'", string.Join(",", newWordRelations.Select(i => i.WordTo.Value).ToArray().Distinct()), word.Value);
                }
            }
            else
            {
                if (db.ChangeTracker.QueryTrackingBehavior == QueryTrackingBehavior.NoTracking)
                {
                    // without tracking we don't have the word value, so use only the wordids
                    if (writer != null) writer.WriteLine("Skipped relating '{0}' to '{1}'", string.Join(",", existingWordRelations.Select(i => i.WordToId).ToArray().Distinct()), word.Value);
                }
                else
                {
                    // with tracking we can output the actual words
                    if (writer != null) writer.WriteLine("Skipped relating '{0}' to '{1}'", string.Join(",", existingWordRelations.Select(i => i.WordTo.Value).ToArray().Distinct()), word.Value);
                }
            }
        }

        public static void UpdateState(WordHintDbContext db, string source, Word word, TextWriter writer = null, bool doUseCommentAsKey = false)
        {
            var wordText = word.Value;
            var wordComment = word.Comment;
            var wordLength = word.Value.Length; // use actual word length

            State stateEntity = null;
            if (doUseCommentAsKey)
            {
                wordLength = 0; // ignore using word Length - set to zero
                stateEntity = db.States
                                // .AsNoTracking()
                                .FirstOrDefault(item => item.Source == source && item.Comment == wordComment);
            }
            else
            {
                stateEntity = db.States
                                // .AsNoTracking()
                                .FirstOrDefault(item => item.Source == source && item.NumberOfLetters == wordLength);
            }

            // Validate entity is not null
            if (stateEntity != null)
            {
                stateEntity.CreatedDate = DateTime.Now;
                stateEntity.Word = wordText;

                // add state
                db.States.Update(stateEntity);

                if (writer != null) writer.WriteLine("Updated state with '{0}' as last processed word for '{1}' letters with  source '{2}'.", wordText, wordLength, source);
            }
            else
            {
                stateEntity = new State
                {
                    Word = wordText,
                    NumberOfLetters = wordLength,
                    CreatedDate = DateTime.Now,
                    Source = source,
                    Comment = wordComment
                };

                // add new state
                db.States.Add(stateEntity);

                if (writer != null) writer.WriteLine("Added state with '{0}' as last processed word for '{1}' letters with  source '{2}'.", wordText, wordLength, source);
            }

            // Save changes in database
            db.SaveChanges();

            // detach in order to clean this from the db tracked cache
            db.Entry(stateEntity).State = EntityState.Detached;
        }

        /// <summary>
        /// Get a word id list for all the words that is related to the passed word
        /// </summary>
        /// <param name="db">database</param>
        /// <param name="wordValue">word</param>
        /// <returns>a word id list for all the words that is related to the passed word</returns>
        /// <example>var exludeIds = WordDatabaseService.GetWordIdList(db, "BY");</example>
        public static IEnumerable<int> GetWordIdList(WordHintDbContext db, string wordValue)
        {
            var word = db.Words.SingleOrDefault(w => w.Value == wordValue);
            if (word != null)
            {
                var wordId = word.WordId;

                var relatedWords = db.WordRelations.Where(w => w.WordFromId == word.WordId || w.WordToId == word.WordId)
                                            .AsNoTracking();

                if (relatedWords.Any())
                {
                    var wordList = new List<int>();

                    // build flattened distinct list
                    foreach (var relation in relatedWords)
                    {
                        wordList.Add(relation.WordToId);
                        wordList.Add(relation.WordFromId);
                    }

                    // add main key
                    wordList.Add(wordId);

                    // and sort
                    wordList.Sort();

                    // return distinct ids
                    return wordList.Distinct();
                }
            }
            return new List<int>();
        }

        /// <summary>
        /// Get a word id list for all the words that is related to the passed words
        /// </summary>
        /// <param name="db">database</param>
        /// <param name="wordValues">words</param>
        /// <returns>a word id list for all the words that is related to the passed words</returns>
        /// <example>var exludeIds = WordDatabaseService.GetWordIdList(db, new List<string> { "BY", "NAVN" });</example>
        public static IEnumerable<int> GetWordIdList(WordHintDbContext db, IEnumerable<string> wordValues)
        {
            var words = db.Words.Where(w => wordValues.Contains(w.Value));
            if (words.Any())
            {
                var wordIds = words.Select(w => w.WordId);

                var relatedWords = db.WordRelations.Where(w => wordIds.Contains(w.WordFromId) || wordIds.Contains(w.WordToId))
                                            .AsNoTracking();

                if (relatedWords.Any())
                {
                    var wordList = new List<int>();

                    // build flattened distinct list
                    foreach (var relation in relatedWords)
                    {
                        wordList.Add(relation.WordToId);
                        wordList.Add(relation.WordFromId);
                    }

                    // add main keys
                    wordList.AddRange(wordIds);

                    // and sort
                    wordList.Sort();

                    // return distinct ids
                    return wordList.Distinct();
                }
            }
            return new List<int>();
        }

    }
}