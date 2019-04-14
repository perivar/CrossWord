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

        public override string ToString()
        {
            return string.Format("Id: {0}, Value: {1}, Id: {2}, Value: {3}", WordFromId, WordFrom != null ? WordFrom.Value : "", WordToId, WordTo != null ? WordTo.Value : "");
        }
    }
}