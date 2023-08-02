using System;
using System.Collections.Generic;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class Word : IEquatable<Word>
    {
        public int WordId { get; set; }
        public string Language { get; set; }
        public string Value { get; set; }
        public int NumberOfLetters { get; set; }
        public int NumberOfWords { get; set; }
        public User User { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Source { get; set; }
        public string Comment { get; set; }
        public Category Category { get; set; }


        public virtual ICollection<WordRelation> RelatedTo { get; set; }
        public virtual ICollection<WordRelation> RelatedFrom { get; set; }

        public Word()
        {
            RelatedTo = new List<WordRelation>();
            RelatedFrom = new List<WordRelation>();
        }

        public override string ToString()
        {
            return string.Format("[{0}] Language: {1}, Value: {2}, User: {3}, Date: {4:dd-MM-yyyy}, Source: {5}, Comment: {6}, From: {7}, To: {8}", WordId, Language, Value, User != null ? User.UserName : "", CreatedDate, Source, Comment, RelatedFrom.Count, RelatedTo.Count);
        }

        // implemented IEquatable in order to use Distinct
        // https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type
        public bool Equals(Word other)
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

            return this.Value == other.Value;
        }

        public override int GetHashCode() => this.WordId;
        public override bool Equals(object obj) => this.Equals(obj as Word);
    }
}