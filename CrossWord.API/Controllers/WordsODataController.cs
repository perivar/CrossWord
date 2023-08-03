using Microsoft.AspNetCore.Mvc;
using CrossWord.Scraper.MySQLDbService.Models;
using CrossWord.Scraper.MySQLDbService.Entities;
using Microsoft.AspNetCore.Identity;
using CrossWord.Scraper.MySQLDbService;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;

namespace CrossWord.API.Controllers
{
    [Produces("application/json")]
    [ApiController]
    public class WordsODataController : ODataController
    {
        private readonly IConfiguration config;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly WordHintDbContext db;
        private readonly IApiDescriptionGroupCollectionProvider apiExplorer;

        public WordsODataController(IConfiguration config, UserManager<ApplicationUser> userManager, WordHintDbContext db, IApiDescriptionGroupCollectionProvider apiExplorer)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
            this.apiExplorer = apiExplorer;
        }

        [HttpGet]
        [EnableQuery]
        [Route("odata/Words")]
        public IQueryable<Word> Get()
        {
            if (!Request.Query.ContainsKey("$top"))
            {
                // make sure we always limit somewhat so that we don't ask for the full database
                return db.Words
                        .AsNoTracking()
                        .Take(50)
                        .AsQueryable();
            }
            else
            {
                return db.Words
                        .AsNoTracking()
                        .AsQueryable();
            }
        }

        [HttpGet]
        [EnableQuery]
        [Route("odata/Words({key})")]
        [Route("odata/Words/{key}")]
        public SingleResult<Word> Get([FromRoute] int key)
        {
            var word = db.Words
                            .AsNoTracking()
                            .Where(w => w.WordId == key)
                            .AsQueryable();

            return new SingleResult<Word>(word);
        }

        [HttpGet]
        [EnableQuery]
        [Route("odata/Words/Synonyms(Word={word})")]
        public IQueryable<Word> GetSynonyms([FromRoute] string word)
        {
            word = word.ToUpper();

            var wordElement = db.Words.AsNoTracking().FirstOrDefault(w => w.Value == word);
            if (wordElement == null)
            {
                return Enumerable.Empty<Word>().AsQueryable();
            }

            var wordId = wordElement.WordId;

            // It turned out that two separate queries with a union was much faster than trying to do this in SQL
            var wordRelations1 = db.WordRelations
                                             .AsNoTracking()
                                             .Where(w => w.WordFromId == wordId)
                                             .Select(a => a.WordTo);

            var wordRelations2 = db.WordRelations
                                             .AsNoTracking()
                                             .Where(w => w.WordToId == wordId)
                                             .Select(a => a.WordFrom);

            var wordRelations = wordRelations1.Union(wordRelations2);

            return wordRelations.AsQueryable();
        }

        [HttpGet]
        [EnableQuery]
        [Route("odata/Words/SynonymsPattern(Word={word}, Pattern={pattern})")]
        public IQueryable<Word> GetSynonymsPattern([FromRoute] string word, [FromRoute] string pattern)
        {
            word = word.ToUpper();
            pattern = pattern.ToUpper();

            var wordElement = db.Words.AsNoTracking().FirstOrDefault(w => w.Value == word);
            if (wordElement == null)
            {
                return Enumerable.Empty<Word>().AsQueryable();
            }

            var wordId = wordElement.WordId;

            // It turned out that two separate queries with a union was much faster than trying to do this in SQL
            var wordRelations1 = db.WordRelations
                                        .AsNoTracking()
                                        .Where(w =>
                                            (
                                                (w.WordFromId == wordId)
                                                && EF.Functions.Like(w.WordTo.Value, pattern)
                                                && (w.WordTo.NumberOfLetters == pattern.Length)
                                            )
                                        )
                                        .Select(a => a.WordTo);

            var wordRelations2 = db.WordRelations
                                        .AsNoTracking()
                                        .Where(w =>
                                            (
                                                (w.WordToId == wordId)
                                                && EF.Functions.Like(w.WordFrom.Value, pattern)
                                                && (w.WordFrom.NumberOfLetters == pattern.Length)
                                            )
                                        )
                                        .Select(a => a.WordFrom);

            var wordRelations = wordRelations1.Union(wordRelations2);

            return wordRelations.AsQueryable();
        }
    }
}