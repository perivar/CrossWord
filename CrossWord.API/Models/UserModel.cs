using Newtonsoft.Json;

namespace CrossWord.API.Models
{
    public class UserModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
        
        [JsonProperty("phonenumber")]
        public string PhoneNumber { get; set; }
    }
}