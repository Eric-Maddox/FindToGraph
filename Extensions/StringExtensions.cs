namespace FindToGraph.Extensions
{
    public static class StringExtensions
    {
        public static string StripParagraphTags(this string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            html = html.Trim();
            if (html.StartsWith("<p>", System.StringComparison.OrdinalIgnoreCase) && html.EndsWith("</p>", System.StringComparison.OrdinalIgnoreCase))
            {
                return html.Substring(3, html.Length - 7);
            }
            return html;
        }
    }
}
