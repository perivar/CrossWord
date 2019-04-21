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

        // GET: api/words/5
        [Authorize]
        [HttpGet]
        [Route("api/words/{id}")]
        public IActionResult GetWord(long id)
        {
            var word = db.Words.Where(w => w.WordId == id).SingleOrDefault();
            if (word == null)
            {
                return NotFound(id);
            }

            return Json(word);
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
                    .Select(w => new { w.WordFrom, w.WordTo });

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
            var sortedReturnList = returnList.OrderBy(w => w.NumberOfLetters);

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
                                                .Select(w => new { w.WordFrom, w.WordTo });

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
            var sortedReturnList = returnList.OrderBy(w => w.NumberOfLetters);

            return Json(sortedReturnList);
        }
    }
}