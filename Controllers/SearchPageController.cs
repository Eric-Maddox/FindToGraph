using EPiServer.Web.Mvc;
using FindToGraph.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace FindToGraph.Controllers
{
    public class SearchPageController : PageController<SearchPage>
    {
        public IActionResult Index(SearchPage currentPage, string? q)
        {
            List<string> results = new List<string>();
            if (!string.IsNullOrWhiteSpace(q))
            {
                // Simulate search results
                results.Add($"Result1 for '{q}'");
                results.Add($"Result2 for '{q}'");
                results.Add($"Result3 for '{q}'");
            }
            ViewBag.Query = q;
            ViewBag.Results = results;
            return View(currentPage);
        }
    }
}
