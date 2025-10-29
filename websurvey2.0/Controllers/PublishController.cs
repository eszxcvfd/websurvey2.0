using Microsoft.AspNetCore.Mvc;
using websurvey2._0.Services;
using websurvey2._0.ViewModels;
using websurvey2._0.Repositories;
using websurvey2._0.Models;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Controllers;

public class PublishController : Controller
{
    private readonly IPublishService _publishService;
    private readonly ISurveyRepository _surveys;
    private readonly SurveyDbContext _db;

    public PublishController(IPublishService publishService, ISurveyRepository surveys, SurveyDbContext db)
    {
        _publishService = publishService;
        _surveys = surveys;
        _db = db;
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(HttpContext.Session.GetString("UserId"), out userId);

    // GET: /Publish/Links?surveyId=...
    [HttpGet]
    public async Task<IActionResult> Links(Guid surveyId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var vm = await _publishService.GetChannelsBySurveyAsync(userId, surveyId, ct);
        if (vm is null)
        {
            TempData["Error"] = "Survey not found or access denied.";
            return RedirectToAction("My", "Survey");
        }

        return View(vm);
    }

    // GET: /Publish/CreateLink?surveyId=...
    [HttpGet]
    public async Task<IActionResult> CreateLink(Guid surveyId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var survey = await _surveys.GetByIdAsync(surveyId, ct);
        if (survey is null)
        {
            TempData["Error"] = "Survey not found.";
            return RedirectToAction("My", "Survey");
        }

        var vm = new PublishLinkViewModel
        {
            SurveyId = surveyId,
            SurveyTitle = survey.Title,
            ChannelType = "PublicLink",
            GenerateQrCode = true,
            IsActive = true
        };

        return View(vm);
    }

    // POST: /Publish/CreateLink
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLink([FromForm] PublishLinkViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!TryGetUserId(out var userId))
        {
            if (isAjax) return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            if (isAjax) return BadRequest(new { success = false, errors = errs });
            return View(vm);
        }

        var (success, errors, channel) = await _publishService.CreatePublicLinkAsync(userId, vm, ct);
        if (!success || channel is null)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            foreach (var err in errors) ModelState.AddModelError(string.Empty, err);
            return View(vm);
        }

        if (isAjax)
        {
            return Ok(new
            {
                success = true,
                channelId = channel.ChannelId,
                fullUrl = channel.FullUrl,
                qrImagePath = channel.QrImagePath
            });
        }

        TempData["Success"] = $"Survey link created successfully: {channel.FullUrl}";
        return RedirectToAction(nameof(Links), new { surveyId = vm.SurveyId });
    }

    // POST: /Publish/ToggleStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus([FromForm] Guid channelId, [FromForm] Guid surveyId, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!TryGetUserId(out var userId))
        {
            if (isAjax) return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });
            return RedirectToAction("Login", "Auth");
        }

        // Read current status from DB, then toggle on server
        var meta = await _db.Set<SurveyChannel>()
            .AsNoTracking()
            .Where(c => c.ChannelId == channelId)
            .Select(c => new { c.SurveyId, c.IsActive })
            .FirstOrDefaultAsync(ct);

        if (meta is null)
        {
            if (isAjax) return BadRequest(new { success = false, errors = new[] { "Channel not found." } });
            TempData["Error"] = "Channel not found.";
            return RedirectToAction(nameof(Links), new { surveyId });
        }

        var targetActive = !meta.IsActive;
        var (success, errors) = await _publishService.UpdateChannelStatusAsync(userId, channelId, targetActive, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            TempData["Error"] = string.Join(", ", errors);
            return RedirectToAction(nameof(Links), new { surveyId });
        }

        if (isAjax) return Ok(new { success = true, isActive = targetActive });

        TempData["Success"] = $"Channel status updated to {(targetActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Links), new { surveyId });
    }

    // POST: /Publish/DeleteChannel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChannel([FromForm] Guid channelId, [FromForm] Guid surveyId, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!TryGetUserId(out var userId))
        {
            if (isAjax) return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });
            return RedirectToAction("Login", "Auth");
        }

        var (success, errors) = await _publishService.DeleteChannelAsync(userId, channelId, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            TempData["Error"] = string.Join(", ", errors);
        }
        else
        {
            if (isAjax) return Ok(new { success = true });
            TempData["Success"] = "Channel deleted successfully.";
        }

        return RedirectToAction(nameof(Links), new { surveyId });
    }

    // GET: /Publish/SendEmail?surveyId=...
    [HttpGet]
    public async Task<IActionResult> SendEmail(Guid surveyId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var survey = await _surveys.GetByIdAsync(surveyId, ct);
        if (survey is null)
        {
            TempData["Error"] = "Survey not found.";
            return RedirectToAction("My", "Survey");
        }

        if (survey.Status != "Published")
        {
            TempData["Error"] = "Survey must be published before sending emails.";
            return RedirectToAction("Links", new { surveyId });
        }

        // Get existing public links for this survey
        var channels = await _db.Set<SurveyChannel>()
            .Where(c => c.SurveyId == surveyId && c.ChannelType == "PublicLink" && c.IsActive)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct); // <-- This will now work

        ViewBag.ExistingLinks = channels;

        var vm = new SendEmailViewModel
        {
            SurveyId = surveyId,
            SurveyTitle = survey.Title,
            EmailSubject = $"Survey: {survey.Title}",
            EmailBody = $@"
                <h2>{System.Web.HttpUtility.HtmlEncode(survey.Title)}</h2>
                {(!string.IsNullOrEmpty(survey.Description) ? $"<p>{System.Web.HttpUtility.HtmlEncode(survey.Description)}</p>" : "")}
                <p>We would appreciate your feedback. Please take a few minutes to complete this survey.</p>",
            IncludeSurveyLink = true
        };

        return View(vm);
    }

    // POST: /Publish/SendEmail
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmail([FromForm] SendEmailViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!TryGetUserId(out var userId))
        {
            if (isAjax) return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            if (isAjax) return BadRequest(new { success = false, errors = errs });
            
            // Reload existing links
            var channels = await _db.Set<SurveyChannel>()
                .Where(c => c.SurveyId == vm.SurveyId && c.ChannelType == "PublicLink" && c.IsActive)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync(ct);
            ViewBag.ExistingLinks = channels;
            
            return View(vm);
        }

        var (success, errors, result) = await _publishService.SendEmailCampaignAsync(userId, vm, ct);
        
        if (!success || result is null)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            
            foreach (var err in errors) ModelState.AddModelError(string.Empty, err);
            
            var channels = await _db.Set<SurveyChannel>()
                .Where(c => c.SurveyId == vm.SurveyId && c.ChannelType == "PublicLink" && c.IsActive)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync(ct);
            ViewBag.ExistingLinks = channels;
            
            return View(vm);
        }

        if (isAjax)
        {
            return Ok(new
            {
                success = true,
                message = $"Email campaign sent successfully! {result.SuccessCount}/{result.TotalEmails} emails delivered.",
                result = new
                {
                    result.TotalEmails,
                    result.SuccessCount,
                    result.FailureCount,
                    result.FailedEmails,
                    result.ChannelId
                }
            });
        }

        if (result.FailureCount > 0)
        {
            TempData["Warning"] = $"Email campaign sent with some failures. " +
                                 $"{result.SuccessCount}/{result.TotalEmails} emails delivered. " +
                                 $"Failed: {string.Join(", ", result.FailedEmails)}";
        }
        else
        {
            TempData["Success"] = $"Email campaign sent successfully! {result.SuccessCount} emails delivered.";
        }

        return RedirectToAction(nameof(Links), new { surveyId = vm.SurveyId });
    }
}