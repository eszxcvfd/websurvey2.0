using Microsoft.AspNetCore.Mvc;
using websurvey2._0.Services;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Controllers;

public class QuestionController : Controller
{
    private readonly IQuestionService _questions;
    private readonly IRoleService _roles;

    public QuestionController(IQuestionService questions, IRoleService roles)
    {
        _questions = questions;
        _roles = roles;
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(HttpContext.Session.GetString("UserId"), out userId);

    [HttpGet]
    public async Task<IActionResult> Index(Guid surveyId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var (allowed, _, _) = await _roles.CheckPermissionAsync(userId, surveyId, "EditQuestion", ct);
        if (!allowed) return Forbid();

        var list = await _questions.GetSurveyQuestionsAsync(surveyId, ct);
        ViewBag.SurveyId = surveyId;
        return View(list);
    }

    [HttpGet]
    public IActionResult Create(Guid surveyId)
    {
        if (!TryGetUserId(out _))
            return RedirectToAction("Login", "Auth");

        return View(new QuestionEditViewModel { SurveyId = surveyId, QuestionType = "ShortText" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] QuestionEditViewModel vm, CancellationToken ct)
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

        var (success, errors, q) = await _questions.CreateAsync(userId, vm, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            foreach (var err in errors) ModelState.AddModelError(string.Empty, err);
            return View(vm);
        }

        if (isAjax) return Ok(new { success = true, redirectUrl = Url.Action("Index", new { surveyId = vm.SurveyId }) });
        return RedirectToAction(nameof(Index), new { surveyId = vm.SurveyId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var (ok, error, vm) = await _questions.GetForEditAsync(userId, id, ct);
        if (!ok || vm is null)
        {
            TempData["QError"] = error ?? "Cannot load question.";
            return RedirectToAction("My", "Survey");
        }
        return View("Create", vm); // reuse Create view for editing
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromForm] QuestionEditViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!TryGetUserId(out var userId))
        {
            if (isAjax) return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });
            return RedirectToAction("Login", "Auth");
        }

        if (!vm.QuestionId.HasValue)
        {
            if (isAjax) return BadRequest(new { success = false, errors = new[] { "QuestionId is required." } });
            ModelState.AddModelError(string.Empty, "QuestionId is required.");
            return View("Create", vm);
        }

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            if (isAjax) return BadRequest(new { success = false, errors = errs });
            return View("Create", vm);
        }

        var (success, errors) = await _questions.UpdateAsync(userId, vm, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            foreach (var err in errors) ModelState.AddModelError(string.Empty, err);
            return View("Create", vm);
        }

        if (isAjax) return Ok(new { success = true });
        return RedirectToAction(nameof(Index), new { surveyId = vm.SurveyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid surveyId, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!TryGetUserId(out var userId))
        {
            if (isAjax) return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });
            return RedirectToAction("Login", "Auth");
        }

        var (success, errors) = await _questions.DeleteAsync(userId, id, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            TempData["QError"] = string.Join(", ", errors);
        }
        else
        {
            if (isAjax) return Ok(new { success = true });
            TempData["QSuccess"] = "Question deleted.";
        }
        return RedirectToAction(nameof(Index), new { surveyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder([FromForm] Guid surveyId, [FromForm] List<Guid> orderedIds, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!TryGetUserId(out var userId))
        {
            if (isAjax) return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });
            return RedirectToAction("Login", "Auth");
        }

        var (success, errors) = await _questions.ReorderAsync(userId, surveyId, orderedIds, ct);
        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            TempData["QError"] = string.Join(", ", errors);
        }
        else
        {
            if (isAjax) return Ok(new { success = true });
            TempData["QSuccess"] = "Reordered questions.";
        }
        return RedirectToAction(nameof(Index), new { surveyId });
    }
}