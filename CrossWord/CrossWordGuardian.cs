using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CrossWord.Models
{
    // This model is used by guardian 
    // https://github.com/guardian/frontend/blob/master/common/app/model/CrosswordData.scala
    // and
    // https://github.com/guardian/crosswords-api-scala-client/blob/master/src/main/scala/com/gu/crosswords/api/client/models/Crossword.scala
    // and
    // https://github.com/guardian/frontend/blob/master/static/src/javascripts/__flow__/types/crosswords.js
    public partial class CrossWordGuardian
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("number")]
        public long Number { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("creator")]
        public ICreator? Creator { get; set; }
        [JsonProperty("date")]
        public long Date { get; set; }
        [JsonProperty("entries")]
        public IClue[]? Entries { get; set; }
        [JsonProperty("solutionAvailable")]
        public bool SolutionAvailable { get; set; }
        [JsonProperty("dateSolutionAvailable")]
        public long DateSolutionAvailable { get; set; }
        [JsonProperty("dimensions")]
        public IDimensions? Dimensions { get; set; }
        [JsonProperty("crosswordType")]
        public CrosswordType CrosswordType { get; set; }
        [JsonProperty("pdf")]
        public string? Pdf { get; set; }
        [JsonProperty("instructions")]
        public string? Instructions { get; set; }
    }

    public partial class IClue
    {
        [JsonProperty("id")]
        public string? Id { get; set; }                                  // '1-across',
        [JsonProperty("number")]
        public long Number { get; set; }                                // 1
        [JsonProperty("humanNumber")]
        public string? HumanNumber { get; set; }                         // '1'
        [JsonProperty("clue")]
        public string? Clue { get; set; }                                // 'Toy on a string (2-2)'
        [JsonProperty("direction")]
        public Direction Direction { get; set; }                        // 'across'
        [JsonProperty("length")]
        public long Length { get; set; }                                // 4
        [JsonProperty("group")]
        public string[]? Group { get; set; }                             // ['1-across']
        [JsonProperty("position")]
        public IPosition? Position { get; set; }                         // { x: 0, y: 0 }
        [JsonProperty("separatorLocations")]
        public SeparatorLocations? SeparatorLocations { get; set; }      // { '-': [2] }
        [JsonProperty("solution")]
        public string? Solution { get; set; }                            // YOYO
    }

    public partial class ICreator
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("webUrl")]
        public string? WebUrl { get; set; }
    }

    public partial class IDimensions
    {
        [JsonProperty("cols")]
        public long Cols { get; set; }

        [JsonProperty("rows")]
        public long Rows { get; set; }
    }

    public partial class IPosition
    {
        [JsonProperty("x")]
        public long X { get; set; }

        [JsonProperty("y")]
        public long Y { get; set; }
    }

    public partial class SeparatorLocations
    {
        [JsonProperty("-")]
        public int[]? Dashes;

        [JsonProperty(",")]
        public int[]? Commas;
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum Direction
    {
        [EnumMember(Value = "across")]
        Across,
        [EnumMember(Value = "down")]
        Down
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum CrosswordType
    {
        [EnumMember(Value = "cryptic")]
        Cryptic,
        [EnumMember(Value = "quick")]
        Quick,
        [EnumMember(Value = "quiptic")]
        Quiptic,
        [EnumMember(Value = "prize")]
        PRIZE,
        [EnumMember(Value = "everyman")]
        Everyman,
        [EnumMember(Value = "azed")]
        Azed,
        [EnumMember(Value = "special")]
        Special,
        [EnumMember(Value = "genius")]
        Genius,
        [EnumMember(Value = "speedy")]
        Speedy,
        [EnumMember(Value = "weekend")]
        Weekend
    }

    public partial class CrossWordGuardian
    {
        public static CrossWordGuardian FromJson(string json) => JsonConvert.DeserializeObject<CrossWordGuardian>(json, CrossWordGuardianConverter.Settings);
    }

    public static class CrossWordGuardianSerialize
    {
        public static string ToJson(this CrossWordGuardian self) => JsonConvert.SerializeObject(self, CrossWordGuardianConverter.Settings);
    }

    public static class CrossWordGuardianConverter
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            }
        };
    }
}
