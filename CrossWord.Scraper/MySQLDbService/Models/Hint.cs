using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class Hint
    {
        public int HintId { get; set; }
        public string Language { get; set; }
        public string Value { get; set; }
        public int NumberOfLetters { get; set; }
        public int NumberOfWords { get; set; }
        public User User { get; set; }
        public DateTime CreatedDate { get; set; }
        public ICollection<WordHint> WordHints { get; } = new List<WordHint>();

        public override string ToString()
        {
            return string.Format("Id: {0}, Language: {1}, Value: {2}, WordHints: {3}", HintId, Language, Value, WordHints.Count);
        }
    }
}