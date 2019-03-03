using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CrossWordWeb.Models
{
    public partial class CrossWord
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("editor")]
        public string Editor { get; set; }

        [JsonProperty("copyright")]
        public string Copyright { get; set; }

        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("size")]
        public Size Size { get; set; }


        [JsonProperty("grid")]
        public string[] Grid { get; set; }

        [JsonProperty("gridnums")]
        public long[] Gridnums { get; set; }

        [JsonProperty("circles")]
        public long[] Circles { get; set; }

        [JsonProperty("clues")]
        public Answers Clues { get; set; }

        [JsonProperty("answers")]
        public Answers Answers { get; set; }

        [JsonProperty("notepad")]
        public string Notepad { get; set; }

        [JsonProperty("jnotes")]
        public string Jnotes { get; set; }



        [JsonProperty("acrossmap")]
        public string Acrossmap { get; set; }

        [JsonProperty("admin")]
        public bool Admin { get; set; }

        [JsonProperty("autowrap")]
        public string Autowrap { get; set; }

        [JsonProperty("bbars")]
        public string Bbars { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("dow")]
        public string Dow { get; set; }

        [JsonProperty("downmap")]
        public string Downmap { get; set; }

        [JsonProperty("hold")]
        public string Hold { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("id2")]
        public string Id2 { get; set; }

        [JsonProperty("interpretcolors")]
        public string Interpretcolors { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("mini")]
        public string Mini { get; set; }

        [JsonProperty("rbars")]
        public string Rbars { get; set; }

        [JsonProperty("shadecircles")]
        [JsonConverter(typeof(ParseStringConverter))]
        public bool Shadecircles { get; set; }

        [JsonProperty("track")]
        public string Track { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public partial class Answers
    {
        [JsonProperty("across")]
        public string[] Across { get; set; }

        [JsonProperty("down")]
        public string[] Down { get; set; }
    }

    public partial class Size
    {
        [JsonProperty("cols")]
        public long Cols { get; set; }

        [JsonProperty("rows")]
        public long Rows { get; set; }
    }

    public partial class CrossWord
    {
        public static CrossWord FromJson(string json) => JsonConvert.DeserializeObject<CrossWord>(json, CrossWordWeb.Models.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this CrossWord self) => JsonConvert.SerializeObject(self, CrossWordWeb.Models.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(bool) || t == typeof(bool?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            bool b;
            if (Boolean.TryParse(value, out b))
            {
                return b;
            }
            throw new Exception("Cannot unmarshal type bool");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
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

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
}
