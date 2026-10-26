using System.IO.Packaging;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace JobMatcher.API.Services;

public class DocxTextExtractorService
{
    private readonly ILogger<DocxTextExtractorService> _logger;

    public DocxTextExtractorService(ILogger<DocxTextExtractorService> logger)
    {
        _logger = logger;
    }

    /// <summary>Returns null if the stream is not a valid .docx document.</summary>
    public string? ExtractText(Stream fileStream)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(fileStream, false);
            var body = doc.MainDocumentPart?.Document?.Body;

            if (body == null) return "";

            var paragraphs = body.Descendants<Paragraph>()
                .Select(p => p.InnerText)
                .Where(t => !string.IsNullOrWhiteSpace(t));

            return string.Join("\n", paragraphs);
        }
        catch (Exception ex) when (ex is OpenXmlPackageException or FileFormatException or InvalidDataException)
        {
            // Renamed .pdf, broken archive, etc. This is bad input from the user, not a server error
            _logger.LogWarning(ex, "Uploaded file is not a valid .docx document");
            return null;
        }
    }
}
