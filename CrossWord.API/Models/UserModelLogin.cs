using Newtonsoft.Json;

namespace CrossWord.API.Models
{
    public class UserModelLogin
    {
        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}