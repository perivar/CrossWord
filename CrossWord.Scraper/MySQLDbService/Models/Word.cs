using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class Word
    {
        // https://stackoverflow.com/questions/51308245/ef-core-2-1-self-referencing-entity-with-one-to-many-relationship-generates-add
        public int WordId { get; set; }
        public int? ParentWordId { get; set; }

        public string Language { get; set; }
        public string Value { get; set; }
        public int NumberOfLetters { get; set; }
        public int NumberOfWords { get; set; }
        public long UserId { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual Word Parent { get; set; }
        public virtual ICollection<Word> Synonyms { get; set; }
    }
}