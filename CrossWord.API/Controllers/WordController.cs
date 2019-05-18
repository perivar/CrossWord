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
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace CrossWord.API.Controllers
{
    [Produces("application/json")]
    [ApiController]
    [ApiVersion("1.0")]
    // [Route("api/[controller]/[action]")] // disable the default route and use method specific routes instead
    public class WordController : ControllerBase
    {
        private readonly IConfiguration config;
        private readonly UserManager<IdentityUser> userManager;
        private readonly WordHintDbContext db;
        private readonly IApiDescriptionGroupCollectionProvider apiExplorer;

        public WordController(IConfiguration config, UserManager<IdentityUser> userManager, WordHintDbContext db, IApiDescriptionGroupCollectionProvider apiExplorer)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
            this.apiExplorer = apiExplorer;
        }

        // GET: api/word/5
        [Authorize]
        [HttpGet]
        [Route("api/word/{id}")]
        public IActionResult GetWord(long id)
        {
            var word = db.Words.Where(w => w.WordId == id).SingleOrDefault();
            if (word == null)
            {
                return NotFound(id);
            }

            return Ok(word);
        }

        // GET: api/words
        [Authorize]
        [HttpGet]
        [Route("api/words")]
        public IActionResult GetWords()
        {
            var wordResult = db.Words
                .AsNoTracking()
                .OrderByDescending(p => p.WordId)
                .Take(50)
                ;

            if (!wordResult.Any())
            {
                return NotFound();
            }

            return Ok(wordResult);
        }

        // GET: api/words/query
        [Authorize]
        [HttpGet]
        [Route("api/words/{query}")]
        public IActionResult GetWord(string query)
        {
            query = query.ToUpper();
            var pattern = $"{query}%";

            var wordResult = db.Words
                                    .AsNoTracking()
                                    .Where(w => EF.Functions.Like(w.Value, pattern))
                                    .OrderBy(w => w.NumberOfLetters)
                                    .ThenBy(w => w.Value)
                                    .Select(w => w.Value)
                                    .Take(20);

            if (!wordResult.Any())
            {
                return NotFound(query);
            }

            return Ok(wordResult);
        }

        // GET: api/synonyms/ord
        [Authorize]
        [HttpGet]
        [Route("api/synonyms/{word}")]
        public IActionResult GetWordSynonyms(string word)
        {
            word = word.ToUpper();

            var wordResult = db.Words.Where(w => w.Value == word);
            if (!wordResult.Any())
            {
                return NotFound(word);
            }

            var wordId = wordResult.First().WordId;

            var wordRelations = db.WordRelations
                                            .AsNoTracking()
                                            .Where(w => ((w.WordFromId == wordId) || (w.WordToId == wordId)))
                                            .SelectMany(w => new[] { w.WordFrom, w.WordTo })
                                            .GroupBy(p => p.Value) // to make it distinct
                                            .Select(g => g.First()) // to make it distinct
                                            .Where(w => w.Value != word)
                                            .OrderBy(w => w.NumberOfLetters)
                                            .ThenBy(w => w.Value)
                                            ;

            if (!wordRelations.Any())
            {
                return NotFound($"No synonyms for '{word}' found");
            }

            return Ok(wordRelations);
        }

        // GET: api/synonyms/ord/pattern
        [Authorize]
        [HttpGet]
        [Route("api/synonyms/{word}/{pattern}")]
        public IActionResult GetWordSynonyms(string word, string pattern)
        {
            word = word.ToUpper();
            pattern = pattern.ToUpper();

            var wordResult = db.Words.Where(w => w.Value == word);
            if (!wordResult.Any())
            {
                return NotFound(word);
            }

            var wordId = wordResult.First().WordId;

            var wordRelations = db.WordRelations
                                                .AsNoTracking()
                                                .Where(w => ((w.WordFromId == wordId) || (w.WordToId == wordId))
                                                && (EF.Functions.Like(w.WordFrom.Value, pattern) || EF.Functions.Like(w.WordTo.Value, pattern)))
                                                .SelectMany(w => new[] { w.WordFrom, w.WordTo })
                                                .GroupBy(p => p.Value) // to make it distinct
                                                .Select(g => g.First()) // to make it distinct
                                                .Where(w => w.Value != word && w.NumberOfLetters == pattern.Length) // ensure we only care about the correct values
                                                .OrderBy(w => w.NumberOfLetters)
                                                .ThenBy(w => w.Value)
                                                ;

            if (!wordRelations.Any())
            {
                return NotFound($"No synonyms for '{word}' found");
            }

            return Ok(wordRelations);
        }

        // GET: api/states
        [Authorize]
        [HttpGet]
        [Route("api/states")]
        public IActionResult GetStates()
        {
            var stateResult = db.States
                .OrderByDescending(p => p.NumberOfLetters)
                .ThenBy(a => a.Comment)
                .AsNoTracking();

            if (!stateResult.Any())
            {
                return NotFound();
            }

            return Ok(stateResult);
        }
    }
}