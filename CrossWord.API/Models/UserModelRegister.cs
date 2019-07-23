using Newtonsoft.Json;

namespace CrossWord.API.Models
{
    public class UserModelRegister
    {
        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phonenumber")]
        public string PhoneNumber { get; set; }
    }
}