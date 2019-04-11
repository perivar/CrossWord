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

        public virtual ICollection<WordRelation> RelatedTo { get; set; }
        public virtual ICollection<WordRelation> RelatedFrom { get; set; }

        public Word()
        {
            RelatedTo = new List<WordRelation>();
            RelatedFrom = new List<WordRelation>();
        }


        public override string ToString()
        {
            return string.Format("Id: {0}, Language: {1}, Value: {2}, From: {3}, To: {4}", WordId, Language, Value, RelatedFrom.Count, RelatedTo.Count);
        }
    }
}