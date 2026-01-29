using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Follower.Utils;

public static class HtmlConverter
{
    public static string ToMarkdown(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        var doc = new HtmlDocument();
        doc.OptionPreserveXmlNamespaces = true;
        doc.OptionFixNestedTags = true;
        doc.OptionAutoCloseOnEnd = true;
        doc.OptionOutputAsXml = true;
        doc.LoadHtml(html);

        // Remove script and style elements
        var nodes = doc.DocumentNode.SelectNodes("//script|//style");
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                node.Remove();
            }
        }

        // Remove empty text nodes
        nodes = doc.DocumentNode.SelectNodes("//text()[normalize-space(.)='']");
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                node.Remove();
            }
        }

        var markdown = new StringBuilder();
        ConvertNode(doc.DocumentNode, markdown);
        
        // Clean up extra whitespace
        var result = Regex.Replace(
            Regex.Replace(
                markdown.ToString().Trim(),
                "\n{3,}",  // 3 or more newlines
                "\n\n"
            ),
            " {2,}",      // 2 or more spaces
            " "
        );

        return string.IsNullOrWhiteSpace(result) ? html : result;
    }

    private static void ConvertNode(HtmlNode node, StringBuilder markdown)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Text:
                var text = node.InnerText;
                if (!string.IsNullOrEmpty(text))
                {
                    markdown.Append(text.Replace("\r\n", "\n").Replace("\r", "\n"));
                }
                break;

            case HtmlNodeType.Element:
                ConvertElement(node, markdown);
                break;

            case HtmlNodeType.Document:
                ConvertChildren(node, markdown);
                break;
        }
    }

    private static void ConvertElement(HtmlNode node, StringBuilder markdown)
    {
        switch (node.Name.ToLower())
        {
            case "html":
            case "body":
            case "head":
            case "!doctype":
                ConvertChildren(node, markdown);
                break;

            case "title":
                // Skip title tag
                break;
            case "p":
                markdown.Append("\n\n");
                ConvertChildren(node, markdown);
                markdown.Append("\n\n");
                break;

            case "br":
                markdown.Append("\n");
                break;

            case "h1":
                markdown.Append("\n\n# ");
                ConvertChildren(node, markdown);
                markdown.Append("\n\n");
                break;

            case "h2":
                markdown.Append("\n\n## ");
                ConvertChildren(node, markdown);
                markdown.Append("\n\n");
                break;

            case "h3":
                markdown.Append("\n\n### ");
                ConvertChildren(node, markdown);
                markdown.Append("\n\n");
                break;

            case "h4":
                markdown.Append("\n\n#### ");
                ConvertChildren(node, markdown);
                markdown.Append("\n");
                break;

            case "h5":
                markdown.Append("\n\n##### ");
                ConvertChildren(node, markdown);
                markdown.Append("\n");
                break;

            case "h6":
                markdown.Append("\n\n###### ");
                ConvertChildren(node, markdown);
                markdown.Append("\n");
                break;

            case "strong":
            case "b":
                markdown.Append("**");
                ConvertChildren(node, markdown);
                markdown.Append("**");
                break;

            case "em":
            case "i":
                markdown.Append("*");
                ConvertChildren(node, markdown);
                markdown.Append("*");
                break;

            case "a":
                markdown.Append("[");
                ConvertChildren(node, markdown);
                markdown.Append("](");
                markdown.Append(node.GetAttributeValue("href", ""));
                markdown.Append(")");
                break;

            case "img":
                markdown.Append("![");
                markdown.Append(node.GetAttributeValue("alt", ""));
                markdown.Append("](");
                markdown.Append(node.GetAttributeValue("src", ""));
                markdown.Append(")");
                break;

            case "ul":
                markdown.Append("\n");
                foreach (var child in node.ChildNodes.Where(n => n.Name == "li"))
                {
                    markdown.Append("\n* ");
                    ConvertChildren(child, markdown);
                }
                markdown.Append("\n");
                break;

            case "ol":
                markdown.Append("\n");
                var index = 1;
                foreach (var child in node.ChildNodes.Where(n => n.Name == "li"))
                {
                    markdown.Append($"\n{index}. ");
                    ConvertChildren(child, markdown);
                    index++;
                }
                markdown.Append("\n");
                break;

            case "code":
                markdown.Append("`");
                ConvertChildren(node, markdown);
                markdown.Append("`");
                break;

            case "pre":
                markdown.Append("\n```\n");
                ConvertChildren(node, markdown);
                markdown.Append("\n```\n");
                break;

            case "blockquote":
                var lines = node.InnerText.Split('\n');
                markdown.Append("\n");
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        markdown.Append("> ").Append(line.Trim()).Append("\n");
                    }
                }
                break;

            case "hr":
                markdown.Append("\n---\n");
                break;

            case "table":
                ConvertTable(node, markdown);
                break;

            default:
                ConvertChildren(node, markdown);
                break;
        }
    }

    private static void ConvertChildren(HtmlNode node, StringBuilder markdown)
    {
        foreach (var child in node.ChildNodes)
        {
            ConvertNode(child, markdown);
        }
    }

    private static void ConvertTable(HtmlNode table, StringBuilder markdown)
    {
        var rows = table.SelectNodes(".//tr")?.ToList();
        if (rows == null || !rows.Any()) return;

        markdown.Append("\n");

        // Process header
        var headerCells = rows[0].SelectNodes(".//th|.//td")?.ToList();
        if (headerCells != null && headerCells.Any())
        {
            markdown.Append("|");
            foreach (var cell in headerCells)
            {
                markdown.Append(" ").Append(cell.InnerText.Trim()).Append(" |");
            }
            markdown.Append("\n|");
            
            // Add separator row
            foreach (var _ in headerCells)
            {
                markdown.Append(" --- |");
            }
            markdown.Append("\n");
        }

        // Process data rows
        for (var i = 1; i < rows.Count; i++)
        {
            var cells = rows[i].SelectNodes(".//td")?.ToList();
            if (cells == null || !cells.Any()) continue;

            markdown.Append("|");
            foreach (var cell in cells)
            {
                markdown.Append(" ").Append(cell.InnerText.Trim()).Append(" |");
            }
            markdown.Append("\n");
        }

        markdown.Append("\n");
    }
}
