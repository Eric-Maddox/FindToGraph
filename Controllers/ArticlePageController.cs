using EPiServer.Web.Mvc;
using FindToGraph.Models;
using Microsoft.AspNetCore.Mvc;

namespace FindToGraph.Controllers
{
    public class ArticlePageController : PageController<ArticlePage>
    {
        public IActionResult Index(ArticlePage currentPage)
        {
            return View(currentPage);
        }
    }
}
