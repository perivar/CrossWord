using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using System.Linq;

namespace CrossWord.API.Controllers
{

    [Route("api/[controller]")]
    public class CrossWordController : Controller
    {
        private readonly IConfiguration config;
        private readonly UserManager<IdentityUser> userManager;
        private readonly WordHintDbContext db;

        public CrossWordController(IConfiguration config, UserManager<IdentityUser> userManager, WordHintDbContext db)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
        }

        // GET: api/CrossWord
        [Authorize]
        [HttpGet]
        public IActionResult GetCrossWords()
        {
            return NotFound();
        }

        // GET: api/CrossWord/5
        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetCrossWord(long id)
        {
            return NotFound();
        }

    }
}