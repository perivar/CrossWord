using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class Word
    {
        public int WordId { get; set; }
        public string Language { get; set; }
        public string Value { get; set; }
        public int NumberOfLetters { get; set; }
        public int NumberOfWords { get; set; }
        public User User { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Source { get; set; }
        public string Comment { get; set; }

        public virtual ICollection<WordRelation> RelatedTo { get; set; }
        public virtual ICollection<WordRelation> RelatedFrom { get; set; }

        public Word()
        {
            RelatedTo = new List<WordRelation>();
            RelatedFrom = new List<WordRelation>();
        }


        public override string ToString()
        {
            return string.Format("[{0}] Language: {1}, Value: {2}, User: {3}, Date: {4:dd-MM-yyyy}, Source: {5}, Comment: {6}, From: {7}, To: {8}", WordId, Language, Value, User != null ? User.UserName : "", CreatedDate, Source, Comment, RelatedFrom.Count, RelatedTo.Count);
        }
    }
}