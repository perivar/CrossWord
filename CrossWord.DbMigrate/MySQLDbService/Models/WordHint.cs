
namespace CrossWord.DbMigrate.MySQLDbService.Models
{
    public class WordHint
    {
        public int WordId { get; set; }
        public Word Word { get; set; }
        public int HintId { get; set; }
        public Hint Hint { get; set; }
    }
}