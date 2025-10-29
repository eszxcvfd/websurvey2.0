using Microsoft.AspNetCore.Mvc;
using websurvey2._0.Services;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Controllers;

public class BranchLogicController : Controller
{
    private readonly IBranchLogicService _branchLogics;

    public BranchLogicController(IBranchLogicService branchLogics)
    {
        _branchLogics = branchLogics;
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(HttpContext.Session.GetString("UserId"), out userId);

    [HttpGet]
    public async Task<IActionResult> Index(Guid questionId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var vm = await _branchLogics.GetListByQuestionAsync(userId, questionId, ct);
        if (vm is null)
        {
            TempData["Error"] = "Question not found or access denied.";
            return RedirectToAction("My", "Survey");
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] BranchLogicViewModel vm, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            return BadRequest(new { success = false, errors = errs });
        }

        var (success, errors) = await _branchLogics.CreateAsync(userId, vm, ct);
        if (!success)
            return BadRequest(new { success = false, errors });

        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] BranchLogicViewModel vm, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            return BadRequest(new { success = false, errors = errs });
        }

        var (success, errors) = await _branchLogics.UpdateAsync(userId, vm, ct);
        if (!success)
            return BadRequest(new { success = false, errors });

        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] Guid logicId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { success = false, errors = new[] { "Unauthorized" } });

        var (success, errors) = await _branchLogics.DeleteAsync(userId, logicId, ct);
        if (!success)
            return BadRequest(new { success = false, errors });

        return Ok(new { success = true });
    }
}