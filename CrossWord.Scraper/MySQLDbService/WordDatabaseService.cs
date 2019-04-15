using System;
using System.Collections.Generic;
using System.Linq;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossWord.Scraper.MySQLDbService
{
    public static class WordDatabaseService
    {
        public static void AddToDatabase(WordHintDbContext db, User user, string wordText, IEnumerable<string> relatedValues)
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

            AddToDatabase(db, word, relatedWords);
        }

        public static void AddToDatabase(WordHintDbContext db, Word word, IEnumerable<Word> relatedWords)
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
                // Console.WriteLine("Added '{0}' ...", string.Join(",", newRelatedWords.Select(i => i.Value).ToArray()));
            }
            else
            {
                // Console.WriteLine("Skipped adding '{0}' ...", string.Join(",", existingRelatedWords.Select(i => i.Value).ToArray()));
            }

            // what relations needs to be added?
            var allRelatedWords = existingRelatedWords.Concat(newRelatedWords);
            var allWordRelations = allRelatedWords.Select(hint =>
                // only use the ids to speed things up
                new WordRelation { WordFromId = word.WordId, WordToId = hint.WordId }
            );

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
                ||
                (a.WordFromId == wr.WordToId && a.WordToId == wr.WordFromId)
            )).ToList();

            if (newWordRelations.Count > 0)
            {
                db.WordRelations.AddRange(newWordRelations);
                db.SaveChanges();

                // with tracking we can output the actual words
                // Console.WriteLine("Added '{0}' to '{1}' ...", string.Join(",", newRelations.Select(i => i.WordTo.Value).ToArray()), wordText);

                // without tracking we don't have the word value, so use only the wordids
                // Console.WriteLine("Added '{0}' to '{1}' ...", string.Join(",", newRelations.Select(i => i.WordToId).ToArray()), wordText);
            }
            else
            {
                // with tracking we can output the actual words
                // Console.WriteLine("Skipped relating '{0}' to '{1}' ...", string.Join(",", existingRelations.Select(i => i.WordTo.Value).ToArray()), wordText);

                // without tracking we don't have the word value, so use only the wordids
                // Console.WriteLine("Skipped relating '{0}' to '{1}' ...", string.Join(",", existingRelations.Select(i => i.WordToId).ToArray()), wordText);
            }
        }
    }
}