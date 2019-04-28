using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class CrosswordTemplate
    {
        private static readonly char delimiter = ';';
        private string _grid;

        public int CrosswordTemplateId { get; set; }
        public long Cols { get; set; }
        public long Rows { get; set; }


        // https://kimsereyblog.blogspot.com/2017/12/save-array-of-string-entityframework.html
        // https://stackoverflow.com/questions/15220921/how-to-store-double-array-to-database-with-entity-framework-code-first-approac
        [NotMapped]
        public string[] Grid
        {
            get { return _grid.Split(delimiter); }
            set
            {
                _grid = string.Join($"{delimiter}", value);
            }
        }
    }
}