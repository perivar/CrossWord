using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CrossWord.Models
{
    // This model is used by New York Times and XwordInfo
    // see https://www.xwordinfo.com/JSON/
    // and https://github.com/doshea/nyt_crosswords
    public partial class CrossWordTimes
    {
        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("author")]
        public string? Author { get; set; }

        [JsonProperty("editor")]
        public string? Editor { get; set; }

        [JsonProperty("copyright")]
        public string? Copyright { get; set; }

        [JsonProperty("publisher")]
        public string? Publisher { get; set; }

        [JsonProperty("date")]
        public string? Date { get; set; }

        [JsonProperty("size")]
        public Size? Size { get; set; }


        [JsonProperty("grid")]
        public string[]? Grid { get; set; }

        [JsonProperty("gridnums")]
        public long[]? Gridnums { get; set; }

        [JsonProperty("circles")]
        public long[]? Circles { get; set; }

        [JsonProperty("clues")]
        public Answers? Clues { get; set; }

        [JsonProperty("answers")]
        public Answers? Answers { get; set; }

        [JsonProperty("notepad")]
        public string? Notepad { get; set; }

        [JsonProperty("jnotes")]
        public string? Jnotes { get; set; }



        [JsonProperty("acrossmap")]
        public string? Acrossmap { get; set; }

        [JsonProperty("admin")]
        public bool Admin { get; set; }

        [JsonProperty("autowrap")]
        public string? Autowrap { get; set; }

        [JsonProperty("bbars")]
        public string? Bbars { get; set; }

        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("dow")]
        public string? Dow { get; set; }

        [JsonProperty("downmap")]
        public string? Downmap { get; set; }

        [JsonProperty("hold")]
        public string? Hold { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("id2")]
        public string? Id2 { get; set; }

        [JsonProperty("interpretcolors")]
        public string? Interpretcolors { get; set; }

        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("mini")]
        public string? Mini { get; set; }

        [JsonProperty("rbars")]
        public string? Rbars { get; set; }

        [JsonProperty("shadecircles")]
        [JsonConverter(typeof(Boolean2StringJsonConverter))]
        public bool Shadecircles { get; set; }

        [JsonProperty("track")]
        public string? Track { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }
    }

    public partial class Answers
    {
        [JsonProperty("across")]
        public string[]? Across { get; set; }

        [JsonProperty("down")]
        public string[]? Down { get; set; }
    }

    public partial class Size
    {
        [JsonProperty("cols")]
        public long Cols { get; set; }

        [JsonProperty("rows")]
        public long Rows { get; set; }
    }

    public partial class CrossWordTimes
    {
        public static CrossWordTimes FromJson(string? json) => JsonConvert.DeserializeObject<CrossWordTimes>(json, CrossWordTimesConverter.Settings);
    }

    public static class CrossWordTimesSerialize
    {
        public static string ToJson(this CrossWordTimes self) => JsonConvert.SerializeObject(self, CrossWordTimesConverter.Settings);

        public static ICrossBoard ToCrossBoard(this CrossWordTimes self)
        {
            int cols = (int)self.Size.Cols;
            int rows = (int)self.Size.Rows;

            var board = new CrossBoard(cols, rows) as ICrossBoard;

            int n = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var val = self.Grid[n];
                    if (val == ".")
                    {
                        board.AddStartWord(col, row);
                    }

                    n += 1;
                }
            }

            // debug the generated template
            // using (StreamWriter writer = new StreamWriter("template.txt"))
            // {
            //     board.WriteTemplateTo(writer);
            // }

            return board;
        }
    }

    public static class CrossWordTimesConverter
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class Boolean2StringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(bool) || t == typeof(bool?);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            if (bool.TryParse(value, out bool b))
            {
                return b;
            }
            throw new Exception("Cannot unmarshal type bool");
        }

        public override void WriteJson(JsonWriter writer, object? untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (bool)untypedValue;
            var boolString = value ? "true" : "false";
            serializer.Serialize(writer, boolString);
            return;
        }

        public static readonly Boolean2StringJsonConverter Singleton = new();
    }
}
