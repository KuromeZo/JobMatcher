using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace JobMatcher.API.Services;

public class DocxTextExtractorService
{
    public string ExtractText(Stream fileStream)
    {
        using var doc = WordprocessingDocument.Open(fileStream, false);
        var body = doc.MainDocumentPart?.Document?.Body;

        if (body == null) return "";

        var paragraphs = body.Descendants<Paragraph>()
            .Select(p => p.InnerText)
            .Where(t => !string.IsNullOrWhiteSpace(t));

        return string.Join("\n", paragraphs);
    }
}