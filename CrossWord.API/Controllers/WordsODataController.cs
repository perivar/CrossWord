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
                        .Take(50)
                        .AsQueryable();
            }
            else
            {
                return db.Words
                        .AsQueryable();
            }
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key})")]
        public SingleResult<Word> Get(int key)
        {
            var word = db.Words.Where(w => w.WordId == key).AsQueryable();
            return new SingleResult<Word>(word);
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("Synonyms(Word={word})")]
        public IQueryable<Word> GetSynonyms(string word)
        {
            word = word.ToUpper();

            var wordResult = db.Words.Where(w => w.Value == word);
            if (!wordResult.Any())
            {
                return Enumerable.Empty<Word>().AsQueryable();
            }

            var wordId = wordResult.First().WordId;

            var wordRelations = db.WordRelations.Where(w => (w.WordFromId == wordId) || (w.WordToId == wordId))
                                            // .OrderBy(w => w.WordFrom.NumberOfLetters)
                                            .AsNoTracking()
                                            .SelectMany(w => new[] { w.WordFrom, w.WordTo })
                                            // .Distinct()
                                            // .Take(300)
                                            ;

            return wordRelations.AsQueryable();
        }
    }
}