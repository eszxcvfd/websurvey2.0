using Microsoft.AspNetCore.Mvc;
using websurvey2._0.Services;
using websurvey2._0.ViewModels;
using websurvey2._0.Repositories;

namespace websurvey2._0.Controllers;

public class SurveyController : Controller
{
    private readonly ISurveyService _surveyService;
    private readonly ISurveyRepository _surveys;
    private readonly IRoleService _roles;
    private readonly ICollaboratorRepository _collabs;

    public SurveyController(ISurveyService surveyService, ISurveyRepository surveys, IRoleService roles, ICollaboratorRepository collabs)
    {
        _surveyService = surveyService;
        _surveys = surveys;
        _roles = roles;
        _collabs = collabs;
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(HttpContext.Session.GetString("UserId"), out userId);

    // GET: /Survey/Settings?id=...
    [HttpGet]
    public async Task<IActionResult> Settings(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var (allowed, _, _) = await _roles.CheckPermissionAsync(userId, id, "ManageSettings", ct);
        if (!allowed) return Forbid();

        var s = await _surveys.GetByIdAsync(id, ct);
        if (s is null) return NotFound();

        var vm = new SurveySettingsViewModel
        {
            SurveyId = s.SurveyId,
            Title = s.Title,
            Description = s.Description,
            IsAnonymous = s.IsAnonymous,
            DefaultLanguage = s.DefaultLanguage
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettings([FromForm] SurveySettingsViewModel vm, CancellationToken ct)
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
            return View("Settings", vm);
        }

        var (allowed, _, _) = await _roles.CheckPermissionAsync(userId, vm.SurveyId, "ManageSettings", ct);
        if (!allowed)
        {
            if (isAjax) return Forbid();
            return Forbid();
        }

        var (success, errors) = await _surveyService.UpdateSurveySettings(vm.SurveyId, userId, vm, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            foreach (var err in errors) ModelState.AddModelError(string.Empty, err);
            return View("Settings", vm);
        }

        if (isAjax) return Ok(new { success = true, message = "Settings updated." });
        TempData["SettingsSuccess"] = "Settings updated successfully.";
        return RedirectToAction(nameof(Settings), new { id = vm.SurveyId });
    }

    // NEW: GET Schedule page
    [HttpGet]
    public async Task<IActionResult> Schedule(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var (allowed, _, _) = await _roles.CheckPermissionAsync(userId, id, "ManageSettings", ct);
        if (!allowed) return Forbid();

        var s = await _surveys.GetByIdAsync(id, ct);
        if (s is null) return NotFound();

        var vm = new SurveyScheduleViewModel
        {
            SurveyId = s.SurveyId,
            SurveyTitle = s.Title,
            OpenAtUtc = s.OpenAtUtc,
            CloseAtUtc = s.CloseAtUtc,
            ResponseQuota = s.ResponseQuota,
            QuotaBehavior = s.QuotaBehavior
        };

        return View(vm);
    }

    // NEW: POST Update schedule
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSchedule([FromForm] SurveyScheduleViewModel vm, CancellationToken ct)
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
            return View("Schedule", vm);
        }

        var (allowed, _, _) = await _roles.CheckPermissionAsync(userId, vm.SurveyId, "ManageSettings", ct);
        if (!allowed)
        {
            if (isAjax) return Forbid();
            return Forbid();
        }

        var (success, errors) = await _surveyService.UpdateScheduleAsync(vm.SurveyId, userId, vm, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            foreach (var err in errors) ModelState.AddModelError(string.Empty, err);
            return View("Schedule", vm);
        }

        if (isAjax) return Ok(new { success = true, message = "Schedule updated." });
        TempData["ScheduleSuccess"] = "Schedule updated successfully.";
        return RedirectToAction(nameof(Schedule), new { id = vm.SurveyId });
    }

    [HttpGet]
    public async Task<IActionResult> My(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var owned = await _surveys.GetOwnedByAsync(userId, ct);
        var shared = await _surveys.GetSharedWithAsync(userId, ct);
        var roles = await _collabs.GetRolesForUserAsync(userId, ct);

        var vm = new MySurveysViewModel
        {
            CurrentUserId = userId,
            CurrentUserEmail = HttpContext.Session.GetString("UserEmail"),
            CurrentUserName = HttpContext.Session.GetString("FullName"),
            Surveys = owned,
            SharedSurveys = shared,
            SharedRoles = roles
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ShareAccess(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var survey = await _surveys.GetByIdAsync(id, ct);
        if (survey is null) return NotFound();

        var (ok, errors, collaborators) = await _roles.GetCollaboratorsAsync(userId, id, ct);
        if (!ok)
        {
            TempData["ShareError"] = string.Join(", ", errors);
            return RedirectToAction(nameof(My));
        }

        ViewBag.Survey = survey;
        ViewBag.Collaborators = collaborators;
        return View(new AssignRoleViewModel { SurveyId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole([FromForm] AssignRoleViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        if (!TryGetUserId(out var actingId))
        {
            if (isAjax) return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            if (isAjax) return BadRequest(new { success = false, errors = errs });
            TempData["ShareError"] = string.Join(", ", errs);
            return RedirectToAction(nameof(ShareAccess), new { id = vm.SurveyId });
        }

        var (success, errors) = await _roles.AssignRoleAsync(actingId, vm.SurveyId, vm.TargetUserId, vm.Role, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            TempData["ShareError"] = string.Join(", ", errors);
        }
        else
        {
            if (isAjax) return Ok(new { success = true });
            TempData["ShareSuccess"] = "Role assigned.";
        }
        return RedirectToAction(nameof(ShareAccess), new { id = vm.SurveyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCollaborator([FromForm] Guid surveyId, [FromForm] Guid userId, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        if (!TryGetUserId(out var actingId))
        {
            if (isAjax) return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });
            return RedirectToAction("Login", "Auth");
        }

        var (success, errors) = await _roles.RemoveRoleAsync(actingId, surveyId, userId, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            TempData["ShareError"] = string.Join(", ", errors);
        }
        else
        {
            if (isAjax) return Ok(new { success = true });
            TempData["ShareSuccess"] = "Access removed.";
        }
        return RedirectToAction(nameof(ShareAccess), new { id = surveyId });
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!TryGetUserId(out _))
            return RedirectToAction("Login", "Auth");

        return View(new SurveyCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] SurveyCreateViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!TryGetUserId(out var ownerId))
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

        var (success, errors, survey) = await _surveyService.CreateDraftSurvey(ownerId, vm.Title, vm.Lang, ct);
        if (!success || survey is null)
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
                surveyId = survey.SurveyId,
                redirectUrl = Url.Action("Index", "Home")
            });
        }

        TempData["CreateSurveySuccess"] = "Survey created as Draft.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var (allowed, errors, role) = await _roles.CheckPermissionAsync(userId, id, "Publish", ct);
        if (!allowed)
        {
            TempData["Error"] = string.Join(", ", errors);
            return RedirectToAction("My");
        }

        var (success, publishErrors) = await _surveyService.PublishSurveyAsync(id, userId, ct);
        if (!success)
        {
            TempData["Error"] = string.Join(", ", publishErrors);
            return RedirectToAction("My");
        }

        TempData["Success"] = "Survey published successfully!";
        return RedirectToAction("My");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var (allowed, errors, role) = await _roles.CheckPermissionAsync(userId, id, "Publish", ct);
        if (!allowed)
        {
            TempData["Error"] = string.Join(", ", errors);
            return RedirectToAction("My");
        }

        var (success, closeErrors) = await _surveyService.CloseSurveyAsync(id, userId, ct);
        if (!success)
        {
            TempData["Error"] = string.Join(", ", closeErrors);
            return RedirectToAction("My");
        }

        TempData["Success"] = "Survey closed successfully!";
        return RedirectToAction("My");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var (allowed, errors, role) = await _roles.CheckPermissionAsync(userId, id, "Publish", ct);
        if (!allowed)
        {
            TempData["Error"] = string.Join(", ", errors);
            return RedirectToAction("My");
        }

        var (success, reopenErrors) = await _surveyService.ReopenSurveyAsync(id, userId, ct);
        if (!success)
        {
            TempData["Error"] = string.Join(", ", reopenErrors);
            return RedirectToAction("My");
        }

        TempData["Success"] = "Survey reopened successfully!";
        return RedirectToAction("My");
    }
}