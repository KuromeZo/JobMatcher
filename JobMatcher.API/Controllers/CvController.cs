using JobMatcher.API.Infrastructure;
using JobMatcher.API.Services;
using JobMatcher.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobMatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CvController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;

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
    [RequestSizeLimit(MaxFileSize)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public async Task<IActionResult> UploadCv(IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "No file uploaded");

        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Only .docx files are supported");

        await using var stream = file.OpenReadStream();
        var text = _extractor.ExtractText(stream);

        if (text == null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The file is not a valid .docx document");

        if (string.IsNullOrWhiteSpace(text))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Could not extract text from document");

        _logger.LogInformation("Extracted {Length} characters from CV", text.Length);

        // AiServiceException is turned into a 502 by GlobalExceptionHandler
        var profile = await _parser.ParseCvAsync(text, ct);

        return Ok(profile);
    }
}
