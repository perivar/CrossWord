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

        public bool HasValidRefreshToken(string refreshToken)
        {
            return RefreshTokens.Any(rt => rt.Token == refreshToken && rt.Active);
        }

        public void AddRefreshToken(string token, string remoteIpAddress, double daysToExpire = 5)
        {
            //if (RefreshTokens.Any(r => !string.IsNullOrEmpty(remoteIpAddress) && r.RemoteIpAddress == remoteIpAddress))
            if (RefreshTokens.Any(r => r.ApplicationUserId == this.Id))
            {
                // update existing
                // var existingToken = RefreshTokens.First(a => a.RemoteIpAddress == remoteIpAddress);
                var existingToken = RefreshTokens.First(a => a.ApplicationUserId == this.Id);
                existingToken.RemoteIpAddress = remoteIpAddress;
                existingToken.Token = token;
                existingToken.Expires = DateTime.UtcNow.AddDays(daysToExpire);
                existingToken.Modified = DateTime.UtcNow;
            }
            else
            {
                // add new
                var newToken = new RefreshToken(token, DateTime.UtcNow.AddDays(daysToExpire), this, remoteIpAddress);
                newToken.Created = DateTime.UtcNow;
                RefreshTokens.Add(newToken);
            }
        }

        public void RemoveRefreshToken(string refreshToken)
        {
            RefreshTokens.Remove(RefreshTokens.First(t => t.Token == refreshToken));
        }
    }
}