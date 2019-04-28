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
using System.Diagnostics;
using System.Net;
using CrossWord.Models;

namespace CrossWord.API.Controllers
{

    // [Route("api/[controller]")]
    public class CrossWordController : Controller
    {
        private readonly IConfiguration config;
        private readonly UserManager<IdentityUser> userManager;
        private readonly WordHintDbContext db;
        private readonly ICrossDictionary dictionary;

        public CrossWordController(IConfiguration config, UserManager<IdentityUser> userManager, WordHintDbContext db, ICrossDictionary dictionary)
        {
            this.config = config;
            this.userManager = userManager;
            this.db = db;
            this.dictionary = dictionary;
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

        // GET: api/crosswords
        // [Authorize]
        [HttpGet]
        [Route("api/crosswords")]
        public IActionResult GetCrossWords()
        {
            // Start the stopwatch   
            var watch = new Stopwatch();
            watch.Start();

            ICrossBoard board = null;
            var template = db.CrosswordTemplates.FirstOrDefault();
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

            CrossWord.Models.CrossWordModel crossword;
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
            return Json(crossword);
        }

        // GET: api/crosswords/5
        // [Authorize]
        [HttpGet]
        [Route("api/crosswords/{id}")]
        public IActionResult GetCrossWord(long id)
        {
            return NotFound();
        }

    }
}