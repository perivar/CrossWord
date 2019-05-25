using System;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using CrossWord.Scraper.MySQLDbService;
using static Microsoft.AspNet.OData.Query.AllowedQueryOptions;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CrossWord.API.Controllers
{
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [ODataRoutePrefix("Words")]
    public class WordsODataController : ODataController
    {
        private readonly IConfiguration config;
        private readonly UserManager<IdentityUser> userManager;
        private readonly WordHintDbContext db;
        private readonly IApiDescriptionGroupCollectionProvider apiExplorer;

        public WordsODataController(IConfiguration config, UserManager<IdentityUser> userManager, WordHintDbContext db, IApiDescriptionGroupCollectionProvider apiExplorer)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
            this.apiExplorer = apiExplorer;
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute]
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
        [ODataRoute("({key})")]
        public SingleResult<Word> Get([FromODataUri] int key)
        {
            var word = db.Words
                            .AsNoTracking()
                            .Where(w => w.WordId == key)
                            .AsQueryable();

            return new SingleResult<Word>(word);
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("Synonyms(Word={word})")]
        public IQueryable<Word> GetSynonyms([FromODataUri] string word)
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
        [ODataRoute("Synonyms(Word={word}, Pattern={pattern})")]
        public IQueryable<Word> GetSynonyms([FromODataUri] string word, [FromODataUri] string pattern)
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
                                                    && (EF.Functions.Like(w.WordTo.Value, pattern))
                                                    && (w.WordTo.NumberOfLetters == pattern.Length)
                                                )
                                            )
                                            .Select(a => a.WordTo);

            var wordRelations2 = db.WordRelations
                                            .AsNoTracking()
                                            .Where(w =>
                                                (
                                                    (w.WordToId == wordId)
                                                    && (EF.Functions.Like(w.WordFrom.Value, pattern))
                                                    && (w.WordFrom.NumberOfLetters == pattern.Length)
                                                )
                                            )
                                            .Select(a => a.WordFrom);

            var wordRelations = wordRelations1.Union(wordRelations2);

            return wordRelations.AsQueryable();
        }

    }
}