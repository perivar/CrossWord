using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace CrossWord.Scraper.MySQLDbService.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public long? FacebookId { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } // navigation property

        public ApplicationUser()
        {
            RefreshTokens = new List<RefreshToken>();
        }

        public RefreshToken GetRefreshToken(string refreshToken)
        {
            return RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);
        }

        public bool HasValidRefreshToken(string refreshToken)
        {
            return RefreshTokens.Any(rt => rt.Token == refreshToken && rt.Active);
        }

        public void AddRefreshToken(string token, string remoteIpAddress, string userAgent, double daysToExpire = 5)
        {
            if (RefreshTokens.Any(r =>
                !string.IsNullOrEmpty(remoteIpAddress) && r.RemoteIpAddress == remoteIpAddress
                &&
                !string.IsNullOrEmpty(userAgent) && r.UserAgent == userAgent
            ))
            // if (RefreshTokens.Any(r => r.ApplicationUserId == this.Id))
            {
                // update existing
                var existingToken = RefreshTokens.First(a => a.RemoteIpAddress == remoteIpAddress && a.UserAgent == userAgent);
                // var existingToken = RefreshTokens.First(a => a.ApplicationUserId == this.Id);
                existingToken.Token = token;
                existingToken.Expires = DateTime.UtcNow.AddDays(daysToExpire);
                existingToken.Modified = DateTime.UtcNow;
            }
            else
            {
                // add new
                var newToken = new RefreshToken(token, DateTime.UtcNow.AddDays(daysToExpire), this, remoteIpAddress, userAgent)
                {
                    Created = DateTime.UtcNow
                };
                RefreshTokens.Add(newToken);
            }
        }

        public void RemoveRefreshToken(string refreshToken)
        {
            RefreshTokens.Remove(RefreshTokens.First(t => t.Token == refreshToken));
        }
    }
}