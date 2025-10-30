using Microsoft.AspNetCore.Mvc;
using websurvey2._0.Services;
using websurvey2._0.ViewModels;
using Microsoft.EntityFrameworkCore; // ADD THIS
using websurvey2._0.Models; // ADD THIS

namespace websurvey2._0.Controllers;

public class RespondController : Controller
{
    private readonly IResponseService _responseService;
    private readonly ILogger<RespondController> _logger;
    private readonly SurveyDbContext _db; // ADD THIS

    public RespondController(
        IResponseService responseService, 
        ILogger<RespondController> logger,
        SurveyDbContext db) // ADD THIS
    {
        _responseService = responseService;
        _logger = logger;
        _db = db; // ADD THIS
    }

    // GET: /Respond/ShowSurvey?surveyId=...&channelId=...
    [HttpGet]
    public async Task<IActionResult> ShowSurvey(Guid surveyId, Guid? channelId, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, error, model) = await _responseService.GetSurveyForResponseAsync(surveyId, channelId, ipAddress, ct);

        if (!success || model is null)
        {
            ViewBag.ErrorMessage = error ?? "Unable to load survey.";
            return View("Error");
        }

        // Generate anti-spam token
        ViewBag.AntiSpamToken = GenerateSimpleToken();

        return View(model);
    }

    // POST: /Respond/SubmitResponse
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitResponse([FromForm] SubmitResponseViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            if (isAjax) return BadRequest(new { success = false, errors = errs });

            ViewBag.ErrorMessage = string.Join(", ", errs);
            return View("Error");
        }

        var (success, errors, responseId) = await _responseService.SubmitResponseAsync(vm, ipAddress, ct);

        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });

            ViewBag.ErrorMessage = string.Join(", ", errors);
            return View("Error");
        }

        if (isAjax)
        {
            return Ok(new { success = true, responseId, message = "Thank you for your response!" });
        }

        TempData["SuccessMessage"] = "Thank you for completing the survey!";
        return RedirectToAction(nameof(ThankYou), new { responseId });
    }

    // GET: /Respond/ThankYou?responseId=...
    [HttpGet]
    public IActionResult ThankYou(Guid? responseId)
    {
        ViewBag.ResponseId = responseId;
        return View();
    }

    // Add this new method to handle /s/{slug} URLs
    // GET: /s/{slug}
    [HttpGet("/s/{slug}")]
    public async Task<IActionResult> ShowSurveyBySlug(string slug, CancellationToken ct)
    {
        // Look up the channel by slug
        var channel = await _db.Set<SurveyChannel>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PublicUrlSlug == slug && c.IsActive, ct);

        if (channel is null)
        {
            ViewBag.ErrorMessage = "Survey link not found or has been deactivated.";
            return View("Error");
        }

        // Redirect to the standard ShowSurvey action with surveyId and channelId
        return RedirectToAction(nameof(ShowSurvey), new 
        { 
            surveyId = channel.SurveyId, 
            channelId = channel.ChannelId 
        });
    }

    private static string GenerateSimpleToken()
    {
        return Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks.ToString("X");
    }
}