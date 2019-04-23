using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class State
    {
        public int StateId { get; set; }
        public int WordId { get; set; }
        public virtual Word Word { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Source { get; set; }
        public string Comment { get; set; }


        public override string ToString()
        {
            return string.Format("Id: {0}, WordId: {1}, Value: {2}, Date: {3:dd-MM-yyyy}, Source: {4}, Comment: {5}", StateId, WordId, Word != null ? Word.Value : "", CreatedDate, Source, Comment);
        }

    }
}