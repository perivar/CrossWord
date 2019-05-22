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
using System.Globalization;

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
                .Take(300)
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

        // DELETE: api/words/5
        // [Authorize(Roles = "Admin")]
        [Authorize]
        [HttpDelete]
        [Route("api/words/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var word = await db.Words.FindAsync(id);

            if (word == null)
            {
                return NotFound();
            }

            var wordRelations = await db.WordRelations
                .Where(e => id == e.WordFromId || id == e.WordToId)
                .ToListAsync();

            // first delete the relations
            db.WordRelations.RemoveRange(wordRelations);

            // then delete the actual word
            db.Words.Remove(word);
            await db.SaveChangesAsync();

            return Ok(word);
        }

        // DELETE: /api/words/delete?id=1&id=2&id=3
        // [Authorize(Roles = "Admin")]
        [Authorize]
        [HttpDelete]
        [Route("api/words/delete")]
        public async Task<IActionResult> Delete(int[] id)
        {
            var words = await db.Words
                .Where(e => id.Contains(e.WordId))
                .ToListAsync();

            if (words == null)
            {
                return NotFound();
            }

            var wordRelations = await db.WordRelations
                .Where(e => id.Contains(e.WordFromId) || id.Contains(e.WordToId))
                .ToListAsync();

            // first delete the relations
            db.WordRelations.RemoveRange(wordRelations);

            // then delete the actual words
            db.Words.RemoveRange(words);
            await db.SaveChangesAsync();

            return Ok(words);
        }

        // PUT: /api/words/5
        [Authorize]
        [HttpPut]
        [Route("api/words/{id}")]
        public async Task<IActionResult> PutWord(long id, Word item)
        {
            if (id != item.WordId)
            {
                return BadRequest();
            }

            db.Entry(item).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return NoContent();
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
        // [Authorize]
        [HttpGet]
        [Route("api/states")]
        public IActionResult GetStates()
        {
            // in order to sort with Collation we need to use raw SQL
            // however this requires the db field to have the right collation, which it doesn't
            // var stateResult = db.States.FromSql(
            //     $"SELECT * FROM States AS s ORDER BY s.NumberOfLetters DESC, s.Comment ASC COLLATE utf8mb4_da_0900_as_cs")
            //     .AsNoTracking();

            // var stateResult = db.States
            //     .OrderByDescending(p => p.NumberOfLetters)
            //     .ThenBy(a => a.Comment)
            //     .AsNoTracking();

            // sort in memory since the collation will not work
            CultureInfo culture = new CultureInfo("no");
            var stateResult = db.States
                .AsNoTracking()
                .AsEnumerable() // force sorting in memory since the string comparer isn't supported directly in ef core
                .OrderByDescending(p => p.NumberOfLetters)
                .ThenBy(a => a.Comment, StringComparer.Create(culture, true));

            if (!stateResult.Any())
            {
                return NotFound();
            }

            return Ok(stateResult);
        }
    }
}