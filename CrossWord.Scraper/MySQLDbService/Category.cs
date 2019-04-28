using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossWord.Scraper.MySQLDbService
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Language { get; set; }
        public string Value { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Source { get; set; }
        public string Comment { get; set; }

    }
}