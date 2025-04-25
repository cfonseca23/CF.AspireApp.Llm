using System.Text.RegularExpressions;

using HtmlAgilityPack;

namespace ConsoleApp.Utilities
{
    public static class HtmlExtractor
    {
        public static string ExtractText(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            htmlDoc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

            var text = htmlDoc.DocumentNode.InnerText;
            return CleanText(text);
        }

        private static string CleanText(string text)
        {
            var cleanText = HtmlEntity.DeEntitize(text);
            return Regex.Replace(cleanText, @"\s+", " ").Trim();
        }
    }
}
