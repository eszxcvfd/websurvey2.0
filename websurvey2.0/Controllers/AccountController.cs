using Microsoft.AspNetCore.Mvc;
using websurvey2._0.Services;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _account;

    public AccountController(IAccountService account)
    {
        _account = account;
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(HttpContext.Session.GetString("UserId"), out userId);

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        var (ok, error, profile) = await _account.GetProfileAsync(userId, ct);
        if (!ok || profile is null)
        {
            TempData["ProfileError"] = error ?? "Unable to load profile.";
            return RedirectToAction("Index", "Home");
        }
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] ProfileViewModel vm, CancellationToken ct)
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
            return View("Profile", vm);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var (success, errors) = await _account.UpdateProfileAsync(userId, vm, ip, ua, ct);

        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            foreach (var err in errors) ModelState.AddModelError(string.Empty, err);
            return View("Profile", vm);
        }

        if (isAjax) return Ok(new { success = true, message = "Profile updated." });
        TempData["ProfileSuccess"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> ChangePassword(CancellationToken ct)
    {
        if (!TryGetUserId(out var _))
            return RedirectToAction("Login", "Auth");

        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordViewModel vm, CancellationToken ct)
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

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var (success, errors) = await _account.ChangePasswordAsync(userId, vm, ip, ua, ct);

        if (!success)
        {
            if (isAjax) return BadRequest(new { success = false, errors });
            foreach (var err in errors) ModelState.AddModelError(string.Empty, err);
            return View(vm);
        }

        if (isAjax) return Ok(new { success = true, redirectUrl = Url.Action("Profile", "Account") });
        TempData["ChangePwdSuccess"] = "Password changed successfully.";
        return RedirectToAction(nameof(Profile));
    }
}