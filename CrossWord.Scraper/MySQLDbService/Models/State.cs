using System;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class State
    {
        public int StateId { get; set; }
        public string Word { get; set; }
        public int NumberOfLetters { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Source { get; set; }
        public string Comment { get; set; }


        public override string ToString()
        {
            return string.Format("Id: {0}, Word: {1}, Letters: {2}, Date: {3:dd-MM-yyyy}, Source: {4}, Comment: {5}", StateId, Word, NumberOfLetters, CreatedDate, Source, Comment);
        }

    }
}