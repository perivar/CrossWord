using System;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class WordRelation : IEquatable<WordRelation>
    {
        public int WordFromId { get; set; }
        public virtual Word WordFrom { get; set; }
        public int WordToId { get; set; }
        public virtual Word WordTo { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Source { get; set; }
        public string Comment { get; set; }


        public override string ToString()
        {
            return string.Format("Id: {0}, Value: {1}, Id: {2}, Value: {3}", WordFromId, WordFrom != null ? WordFrom.Value : "", WordToId, WordTo != null ? WordTo.Value : "");
        }

        // implemented IEquatable in order to use Distinct
        // https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type
        public bool Equals(WordRelation other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            return this.WordFromId == other.WordFromId && this.WordToId == other.WordToId;
        }
        public override int GetHashCode() => (new { this.WordFromId, this.WordToId }).GetHashCode();
        public override bool Equals(object obj) => this.Equals(obj as WordRelation);
    }
}