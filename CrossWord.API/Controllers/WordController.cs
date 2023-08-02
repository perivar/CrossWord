using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using CrossWord.Scraper.MySQLDbService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Globalization;
using CrossWord.Scraper;

namespace CrossWord.API.Controllers
{
    [Produces("application/json")]
    [ApiController]
    [ApiVersion("1.0")]
    // [Route("api/[controller]/[action]")] // disable the default route and use method specific routes instead
    public class WordController : ControllerBase
    {
        private readonly IConfiguration config;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly WordHintDbContext db;
        private readonly IApiDescriptionGroupCollectionProvider apiExplorer;

        public WordController(IConfiguration config, UserManager<ApplicationUser> userManager, WordHintDbContext db, IApiDescriptionGroupCollectionProvider apiExplorer)
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

        // GET: /api/words/disconnect?id=1&id=2&id=3
        // [Authorize(Roles = "Admin")]
        [Authorize]
        [HttpGet]
        [Route("api/words/disconnect")]
        public async Task<IActionResult> Disconnect([FromQuery] string word, [FromQuery] int[] id)
        {
            if (word == null)
            {
                return NotFound();
            }

            word = word.ToUpper();

            var wordResult = db.Words.Where(w => w.Value == word);
            if (!wordResult.Any())
            {
                return NotFound(word);
            }

            var wordId = wordResult.First().WordId;

            var words = await db.Words
                .Where(e => id.Contains(e.WordId))
                .ToListAsync();

            if (words == null)
            {
                return NotFound();
            }

            // disconnect
            var wordRelations = await db.WordRelations
                .Where(
                    (e =>
                        (id.Contains(e.WordFromId) && e.WordToId == wordId)
                        ||
                        (id.Contains(e.WordToId) && e.WordFromId == wordId)
                    )
                )
                .ToListAsync();

            // delete the relations
            db.WordRelations.RemoveRange(wordRelations);
            await db.SaveChangesAsync();

            return Ok(
                new
                {
                    word,
                    wordId,
                    words,
                });
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

            var alreadyExist = db.Words
                                    .AsNoTracking()
                                    .FirstOrDefault(a => a.Value == item.Value);

            if (alreadyExist != null)
            {
                // already have this entry, cannot create an duplicate

                // find all the relations to this word.
                var wordRelations = await db.WordRelations
                    .AsNoTracking()
                    .Where(e => id == e.WordFromId || id == e.WordToId)
                    .ToListAsync();

                if (wordRelations.Any())
                {
                    var allRelatedWordsIds = wordRelations
                                                .SelectMany(w => new[] { w.WordFromId, w.WordToId })
                                                .Distinct()
                                                .Where(w => w != id && w != alreadyExist.WordId)
                                                .OrderBy(w => w)
                                                .ToList()
                                                ;

                    // create new relations to the original word
                    var allWordRelationsFrom = allRelatedWordsIds.Select(wordFromId =>
                        new WordRelation { WordFromId = wordFromId, WordToId = alreadyExist.WordId }
                    );

                    // add relation from each hint to word as well
                    var allWordRelationsTo = allRelatedWordsIds.Select(wordToId =>
                        new WordRelation { WordFromId = alreadyExist.WordId, WordToId = wordToId }
                    );

                    // all relations
                    var allWordRelations = allWordRelationsFrom.Concat(allWordRelationsTo).Distinct();

                    // which relations need to be added?
                    var newWordRelations = allWordRelations.Where(x => !db.WordRelations.Any(z => z.WordFromId == x.WordFromId && z.WordToId == x.WordToId)).ToList();

                    if (newWordRelations.Count > 0)
                    {
                        db.WordRelations.AddRange(newWordRelations);
                    }

                    // then delete the original relations
                    db.WordRelations.RemoveRange(wordRelations);
                }

                // then delete the actual word
                db.Words.Remove(item);
                await db.SaveChangesAsync();

                return Ok(alreadyExist);
            }
            else
            {
                // make sure the counts are recalculated
                var wordText = item.Value;
                item.NumberOfLetters = ScraperUtils.CountNumberOfLetters(wordText);
                item.NumberOfWords = ScraperUtils.CountNumberOfWords(wordText);

                db.Entry(item).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return Ok(item);
            }
        }

        // POST: api/words
        [Authorize]
        [HttpPost]
        [Route("api/words")]
        public async Task<ActionResult<Word>> PostWord(Word item)
        {
            // clean the item, we don't support related words in this way
            item.RelatedFrom = null;
            item.RelatedTo = null;

            var wordText = item.Value;
            item.NumberOfLetters = ScraperUtils.CountNumberOfLetters(wordText);
            item.NumberOfWords = ScraperUtils.CountNumberOfWords(wordText);
            item.Category = null;
            item.CreatedDate = item.CreatedDate == null ? DateTime.Now : item.CreatedDate;

            // use the following statement so that User won't be inserted
            item.User = new User() { UserId = 1 };
            db.Entry(item.User).State = EntityState.Unchanged;

            db.Words.Add(item);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWord), new { id = item.WordId }, item);
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
                                            .Where(w => (w.WordFromId == wordId) || (w.WordToId == wordId))
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

        // GET: api/synonymsbyid/id
        [Authorize]
        [HttpGet]
        [Route("api/synonymsbyid/{id}")]
        public IActionResult GetWordSynonyms(int id)
        {
            var word = db.Words.Find(id);

            if (word == null)
            {
                return NotFound();
            }

            var wordRelations = db.WordRelations
                                            .AsNoTracking()
                                            .Where(w => (w.WordFromId == id) || (w.WordToId == id))
                                            .SelectMany(w => new[] { w.WordFrom, w.WordTo })
                                            .GroupBy(p => p.Value) // to make it distinct
                                            .Select(g => g.First()) // to make it distinct
                                            .Where(w => w.Value != word.Value)
                                            .OrderBy(w => w.NumberOfLetters)
                                            .ThenBy(w => w.Value)
                                            ;

            if (!wordRelations.Any())
            {
                return NotFound($"No synonyms for '{word.Value}' found");
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
            CultureInfo culture = new("no");
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