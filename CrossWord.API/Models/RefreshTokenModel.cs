using Newtonsoft.Json;

namespace CrossWord.API.Models
{
    public class RefreshTokenModel
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }
    }
}