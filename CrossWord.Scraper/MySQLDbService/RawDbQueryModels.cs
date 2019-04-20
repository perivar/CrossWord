namespace CrossWord.Scraper.MySQLDbService
{
    public class WordRelationQueryModel
    {
        public int WordFromId { get; set; }
        public string WordFrom { get; set; }
        public int WordToId { get; set; }
        public string WordTo { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Value: {1}, Id: {2}, Value: {3}", WordFromId, WordFrom, WordToId, WordTo);
        }

    }
}