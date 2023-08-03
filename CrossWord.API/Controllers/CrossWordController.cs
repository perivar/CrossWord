using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using System.Diagnostics;
using CrossWord.Models;
using CrossWord.Scraper.MySQLDbService.Entities;

namespace CrossWord.API.Controllers
{
    [Produces("application/json")]
    [ApiController]
    // [Route("api/[controller]/[action]")] // disable the default route and use method specific routes instead
    public class CrossWordController : ControllerBase
    {
        private readonly IConfiguration config;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly WordHintDbContext db;
        private readonly ICrossDictionary dictionary;
        private readonly ILogger logger;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public IBackgroundTaskQueue Queue { get; }

        public CrossWordController(IConfiguration config,
                                UserManager<ApplicationUser> userManager,
                                WordHintDbContext db,
                                ICrossDictionary dictionary,
                                IBackgroundTaskQueue queue,
                                ILogger<CrossWordController> logger,
                                IServiceScopeFactory serviceScopeFactory)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
            this.dictionary = dictionary;
            this.logger = logger;
            this.Queue = queue;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        // GET: api/crosswords/init
        // [Authorize]
        [HttpGet]
        [Route("api/crosswords/init")]
        public IActionResult InitCrossWordDictionary()
        {
            dictionary.ResetDictionary(25);
            return Ok("CrossWordDictionary was updated");
        }

        private CrosswordTemplate GetRandomCrosswordTemplateFromDb()
        {
            int total = db.CrosswordTemplates.Count();
            if (total == 0) return null;

            Random r = new();
            int offset = r.Next(0, total);

            var result = db.CrosswordTemplates.Skip(offset).FirstOrDefault();
            return result;
        }

        // GET: api/crosswords
        // [Authorize]
        [HttpGet]
        [Route("api/crosswords")]
        public IActionResult GetCrossWords()
        {
            // Start the stopwatch   
            var watch = new Stopwatch();
            watch.Start();

            CrossBoard generated = GetCrossboard();

            CrossWordTimes crossword;
            if (generated == null)
            {
                return NotFound();
            }
            else
            {
                crossword = generated.ToCrossWordModel(dictionary);
            }

            watch.Stop();
            var responseTimeForCompleteRequest = watch.ElapsedMilliseconds;

            crossword.Title = $"Generated in {responseTimeForCompleteRequest} milliseconds";

            // make sure we use the right json serializer settings
            return new JsonResult(crossword, CrossWordTimesConverter.Settings);
        }

        // GET: api/crosswordguardian
        // [Authorize]
        [HttpGet]
        [Route("api/crosswordguardian")]
        public IActionResult GetCrossWordGuardian()
        {
            // Start the stopwatch   
            var watch = new Stopwatch();
            watch.Start();

            CrossBoard generated = GetCrossboard();

            CrossWordGuardian crossword;
            if (generated == null)
            {
                return NotFound();
            }
            else
            {
                crossword = generated.ToCrossWordModelGuardian(dictionary);
            }

            watch.Stop();
            var responseTimeForCompleteRequest = watch.ElapsedMilliseconds;

            crossword.Name = $"Generated in {responseTimeForCompleteRequest} milliseconds";

            // make sure we use the right json serializer settings
            return new JsonResult(crossword, CrossWordGuardianConverter.Settings);
        }

        private CrossBoard GetCrossboard()
        {

            ICrossBoard board = null;
            // var template = GetRandomCrosswordTemplateFromDb();
            CrosswordTemplate template = null;
            if (template != null)
            {
                board = new CrossBoard();

                int cols = (int)template.Cols;
                int rows = (int)template.Rows;

                board.SetBoardSize(cols, rows);

                int n = 0;
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        var val = template.Grid[n];
                        if (val == ".")
                        {
                            board.AddStartWord(col, row);
                        }

                        n += 1;
                    }
                }

                // debug the generated template
                // using (StreamWriter writer = new StreamWriter("template.txt"))
                // {
                //     board.WriteTemplateTo(writer);
                // }
            }
            else
            {
                var model = CrossBoardCreator.GetCrossWordModelFromUrl("http-random");
                board = model.ToCrossBoard();

                // add in database
                var newTemplate = new CrosswordTemplate()
                {
                    Rows = model.Size.Rows,
                    Cols = model.Size.Cols,
                    Grid = model.Grid
                };

                db.CrosswordTemplates.Add(newTemplate);
                db.SaveChanges();
            }

            var gen = new CrossGenerator(dictionary, board);
            board.Preprocess(dictionary);

            var generated = gen.Generate().FirstOrDefault() as CrossBoard;
            return generated;
        }

        // GET: api/crosswords/5
        // [Authorize]
        [HttpGet]
        [Route("api/crosswords/{id}")]
        public IActionResult GetCrossWord(long id)
        {
            return NotFound();
        }

        // GET: api/templates/generate
        // [Authorize]
        [HttpGet]
        [Route("api/templates/generate")]
        public IActionResult GenerateTemplates()
        {
            Queue.QueueBackgroundWorkItem(async token =>
            {
                var guid = Guid.NewGuid().ToString();
                logger.LogInformation(
                    $"Queued Background Task {guid} added to the queue.");

                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<WordHintDbContext>();

                    try
                    {
                        var model = CrossBoardCreator.GetCrossWordModelFromUrl("http-random");
                        var board = model.ToCrossBoard();

                        // add in database
                        var newTemplate = new CrosswordTemplate()
                        {
                            Rows = model.Size.Rows,
                            Cols = model.Size.Cols,
                            Grid = model.Grid
                        };

                        db.CrosswordTemplates.Add(newTemplate);
                        await db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "An error occurred writing to the " +
                            $"database. Error: {ex.Message}");
                    }
                }

                logger.LogInformation(
                    $"Queued Background Task {guid} is complete.");
            });

            return Ok("Generate crosssword template added to the queue");
        }
    }
}