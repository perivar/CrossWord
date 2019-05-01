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

namespace CrossWord.API.Controllers
{
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ApiVersionNeutral]
    // [ApiVersion("2.0")]
    [Route("words")]
    [ODataRoutePrefix("words")]
    public class Word2Controller : ODataController
    {
        private readonly IConfiguration config;
        private readonly UserManager<IdentityUser> userManager;
        private readonly WordHintDbContext db;
        private readonly IApiDescriptionGroupCollectionProvider apiExplorer;

        public Word2Controller(IConfiguration config, UserManager<IdentityUser> userManager, WordHintDbContext db, IApiDescriptionGroupCollectionProvider apiExplorer)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
            this.apiExplorer = apiExplorer;
        }

        /// <summary>
        /// Gets a single word.
        /// </summary>
        /// <param name="id">The requested word identifier.</param>
        /// <returns>The requested order.</returns>
        /// <response code="200">The word was successfully retrieved.</response>
        /// <response code="404">The word does not exist.</response>
        // GET: odata/words({id})
        [Authorize]
        [ProducesResponseType(typeof(Word), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        // [EnableQuery(AllowedQueryOptions = Select | Expand)]
        [EnableQuery]
        [HttpGet("{id}")]
        [ODataRoute("({id})")]
        public SingleResult<Word> Get(int id)
        {
            var word = db.Words.Where(w => w.WordId == id).AsQueryable();
            return new SingleResult<Word>(word);
        }

        /// <summary>
        /// Retrieves all words.
        /// </summary>
        /// <returns>All available words.</returns>
        /// <response code="200">Words successfully retrieved.</response>
        /// <response code="400">The word is invalid.</response>
        // GET: odata/words
        [Authorize]
        [ProducesResponseType(typeof(ODataValue<IEnumerable<Word>>), Status200OK)]
        // [EnableQuery(MaxTop = 100, AllowedQueryOptions = Select | Top | Skip | Count | Expand | OrderBy)]
        [EnableQuery]
        [HttpGet]
        [ODataRoute]
        public IQueryable<Word> Get()
        {
            if (!Request.Query.ContainsKey("$top"))
            {
                // make sure we always limit somewhat so that we don't ask for the full database
                return db.Words.Take(50).AsQueryable();
            }
            else
            {
                return db.Words.AsQueryable();
            }
        }
    }
}