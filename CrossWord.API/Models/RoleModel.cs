using Newtonsoft.Json;

namespace CrossWord.API.Models
{
    public class RoleModel
    {
        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }
    }
}