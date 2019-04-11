using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class WordRelation
    {
        public int WordFromId { get; set; }
        public virtual Word WordFrom { get; set; }
        public int WordToId { get; set; }
        public virtual Word WordTo { get; set; }
    }
}