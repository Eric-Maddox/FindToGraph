using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace FindToGraph.Models
{
    [ContentType(DisplayName = "ArticlePage", GUID = "a1b2c3d4-e5f6-7890-abcd-ef1234567890", Description = "Article page with body content")]
    public class ArticlePage : PageData
    {
        [Searchable]
        public virtual XhtmlString? Body { get; set; }
    }
}
