using System;

namespace CrossWord.Scraper.MySQLDbService.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; } // navigation property
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public bool Active => DateTime.UtcNow <= Expires;
        public string RemoteIpAddress { get; set; }

        internal RefreshToken() { /* Required by EF */ }

        public RefreshToken(string token, DateTime expires, string userId, string remoteIpAddress)
        {
            Token = token;
            Expires = expires;
            ApplicationUserId = userId;
            RemoteIpAddress = remoteIpAddress;
        }

        public RefreshToken(string token, DateTime expires, ApplicationUser user, string remoteIpAddress)
        {
            Token = token;
            Expires = expires;
            ApplicationUser = user;
            RemoteIpAddress = remoteIpAddress;
        }
    }
}