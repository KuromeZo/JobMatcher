using JobMatcher.API.Services;
using JobMatcher.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace JobMatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CvController : ControllerBase
{
    private readonly DocxTextExtractorService _extractor;
    private readonly IAiCvParserService _parser;
    private readonly ILogger<CvController> _logger;

    public CvController(
        DocxTextExtractorService extractor,
        IAiCvParserService parser,
        ILogger<CvController> logger)
    {
        _extractor = extractor;
        _parser = parser;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadCv(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .docx files are supported");

        _logger.LogInformation("Processing CV: {FileName}", file.FileName);

        using var stream = file.OpenReadStream();
        var text = _extractor.ExtractText(stream);

        if (string.IsNullOrWhiteSpace(text))
            return BadRequest("Could not extract text from document");

        _logger.LogInformation("Extracted {Length} characters from CV", text.Length);

        var profile = await _parser.ParseCvAsync(text);

        return Ok(profile);
    }
}