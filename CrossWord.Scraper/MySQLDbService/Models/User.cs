using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossWord.Scraper.MySQLDbService.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool isVIP { get; set; }
    }
}