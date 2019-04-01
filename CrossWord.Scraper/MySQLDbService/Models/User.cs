using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class User : IdentityUser
    {
        public User() : base()
        {
        }

        public User(string userName) : base(userName)
        {
        }

        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool isVIP { get; set; }
        public string ExternalId { get; set; }
        
        public override string ToString()
        {
            return string.Format("Id: {0}, ExternalId: {1}, FirstName: {2}, LastName: {3}", Id, ExternalId, FirstName, LastName);
        }
    }
}