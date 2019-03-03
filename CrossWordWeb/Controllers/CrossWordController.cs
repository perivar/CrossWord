using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrossWordWeb.Models;

namespace CrossWordWeb.Controllers
{
    public class CrossWordController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
