using EPiServer.Find;
using EPiServer.Find.Cms;
using EPiServer.Web.Mvc;
using FindToGraph.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using FindToGraph.Extensions;

namespace FindToGraph.Controllers
{
    public class SearchPageController : PageController<SearchPage>
    {
        private readonly IClient _searchClient;

        public SearchPageController(IClient findClient)
        {
            _searchClient = findClient;
        }

        public IActionResult Index(SearchPage currentPage, string? q)
        {
            List<string> results = new List<string>();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var searchResults = _searchClient.Search<ArticlePage>()
                    .For(q).GetContentResult();

                results = searchResults.Select(hit => hit.Body?.ToHtmlString().StripParagraphTags() ?? "No content").ToList();
            }
            ViewBag.Query = q;
            ViewBag.Results = results;
            return View(currentPage);
        }
    }
}
