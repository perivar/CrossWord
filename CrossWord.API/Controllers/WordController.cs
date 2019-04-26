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

namespace CrossWord.API.Controllers
{

    // [Route("api/[controller]/[action]")] // disable the default route and use method specific routes instead
    public class WordController : Controller
    {
        private readonly IConfiguration config;
        private readonly UserManager<IdentityUser> userManager;
        private readonly WordHintDbContext db;

        public WordController(IConfiguration config, UserManager<IdentityUser> userManager, WordHintDbContext db)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
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

            return Json(word);
        }

        // GET: api/words
        [Authorize]
        [HttpGet]
        [Route("api/words")]
        public IActionResult GetWords()
        {
            var wordResult = db.Words.OrderByDescending(p => p.WordId)
                .Take(50)
                .AsNoTracking();

            if (!wordResult.Any())
            {
                return NotFound();
            }

            return Json(wordResult);
        }

        // GET: api/words/query
        [Authorize]
        [HttpGet]
        [Route("api/words/{query}")]
        public IActionResult GetWord(string query)
        {
            query = query.ToUpper();
            var pattern = $"{query}%";

            var wordResult = db.Words.Where(w => EF.Functions.Like(w.Value, pattern))
                                    .OrderBy(w => w.NumberOfLetters)
                                    .ThenBy(w => w.Value)
                                    .AsNoTracking()
                                    .Select(w => w.Value)
                                    .Take(20);

            if (!wordResult.Any())
            {
                return NotFound(query);
            }

            return Json(wordResult);
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

            var wordRelations = db.WordRelations.Where(w => (w.WordFromId == wordId) || (w.WordToId == wordId))
                                            // .OrderBy(w => w.WordFrom.NumberOfLetters)
                                            // .Include(w => w.WordFrom)
                                            // .Include(w => w.WordTo)
                                            .AsNoTracking()
                                            .Select(w => new { w.WordFrom, w.WordTo })
                                            .Take(300);

            if (!wordRelations.Any())
            {
                return NotFound($"No synonyms for '{word}' found");
            }

            // build flattened distinct return list
            // Contains() works because Word implements Equals()
            var returnList = new List<Word>();
            foreach (var relation in wordRelations)
            {
                if (relation.WordFrom.WordId == wordId)
                {
                    if (!returnList.Contains(relation.WordTo)) returnList.Add(relation.WordTo);
                }
                else if (relation.WordTo.WordId == wordId)
                {
                    if (!returnList.Contains(relation.WordFrom)) returnList.Add(relation.WordFrom);
                }
            }
            var sortedReturnList = returnList.OrderBy(w => w.NumberOfLetters).ThenBy(w => w.Value);

            return Json(sortedReturnList);
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

            var wordRelations = db.WordRelations.Where(w => ((w.WordFromId == wordId) || (w.WordToId == wordId))
                                                && (EF.Functions.Like(w.WordFrom.Value, pattern) || EF.Functions.Like(w.WordTo.Value, pattern)))
                                                // .OrderBy(w => w.WordFrom.NumberOfLetters)
                                                // .Include(w => w.WordFrom)
                                                // .Include(w => w.WordTo)
                                                .AsNoTracking()
                                                .Select(w => new { w.WordFrom, w.WordTo })
                                                .Take(300);

            if (!wordRelations.Any())
            {
                return NotFound($"No synonyms for '{word}' found");
            }

            // build flattened distinct return list
            // Contains() works because Word implements Equals()
            var returnList = new List<Word>();
            foreach (var relation in wordRelations)
            {
                if (relation.WordFrom.WordId == wordId)
                {
                    if (!returnList.Contains(relation.WordTo) && relation.WordTo.NumberOfLetters == pattern.Length) returnList.Add(relation.WordTo);
                }
                else if (relation.WordTo.WordId == wordId)
                {
                    if (!returnList.Contains(relation.WordFrom) && relation.WordFrom.NumberOfLetters == pattern.Length) returnList.Add(relation.WordFrom);
                }
            }
            var sortedReturnList = returnList.OrderBy(w => w.NumberOfLetters).ThenBy(w => w.Value);

            return Json(sortedReturnList);
        }

        // GET: api/states
        [Authorize]
        [HttpGet]
        [Route("api/states")]
        public IActionResult GetStates()
        {
            var stateResult = db.States.OrderByDescending(p => p.NumberOfLetters)
                .AsNoTracking();

            if (!stateResult.Any())
            {
                return NotFound();
            }

            return Json(stateResult);
        }
    }
}