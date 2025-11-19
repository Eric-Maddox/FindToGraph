using EPiServer.Web.Mvc;
using FindToGraph.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FindToGraph.Extensions;
using StrawberryShake;


namespace FindToGraph.Controllers
{
    public class SearchPageController : PageController<SearchPage>
    {
        private readonly IGraphQLClient _searchClient;

        public SearchPageController(IGraphQLClient graphClient)
        {
            _searchClient = graphClient;
        }

        public async Task<IActionResult> Index(SearchPage currentPage, string? q)
        {
            List<string> results = new List<string>();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var searchResult = await _searchClient.Articles.ExecuteAsync(q);
                searchResult.EnsureNoErrors();
                var items = searchResult.Data?.ArticlePage?.Items;
                if (items != null) {
                    results = items.Select(item => item?.Body?.Html?.StripParagraphTags() ?? "No content").ToList();
                }
            }

            ViewBag.Query = q;
            ViewBag.Results = results;
            return View(currentPage);
        }
    }
}
