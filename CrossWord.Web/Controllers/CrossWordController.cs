using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrossWord.Web.Models;

namespace CrossWord.Web.Controllers
{
    public class CrossWordController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
