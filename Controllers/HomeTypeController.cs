using EPiServer.Web.Mvc;
using FindToGraph.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace FindToGraph.Controllers
{
    public class HomeTypeController : PageController<HomeType>
    {
        public IActionResult Index(HomeType currentPage)
        {
            return View(currentPage);
        }
    }
}
