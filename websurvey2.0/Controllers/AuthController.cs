using Microsoft.AspNetCore.Mvc;
using websurvey2._0.Services;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService auth, ILogger<AuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();

            if (isAjax)
            {
                return BadRequest(new { success = false, errors = errs });
            }
            return View(vm);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var (success, errors, user) = await _auth.LoginAsync(vm, ip, ua, ct);

        if (!success || user is null)
        {
            if (isAjax)
            {
                return BadRequest(new { success = false, errors });
            }
            foreach (var err in errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(vm);
        }

        HttpContext.Session.SetString("UserId", user.UserId.ToString());
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("FullName", user.FullName ?? string.Empty);

        if (isAjax)
        {
            return Ok(new { success = true, redirectUrl = Url.Action("My", "Survey") });
        }
        return RedirectToAction("My", "Survey");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromForm] RegisterViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();

            if (isAjax)
            {
                return BadRequest(new { success = false, errors = errs });
            }
            return View(vm);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var (success, errors) = await _auth.RegisterAsync(vm, ip, ua, ct);

        if (!success)
        {
            if (isAjax)
            {
                return BadRequest(new { success = false, errors });
            }
            foreach (var err in errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(vm);
        }

        if (isAjax)
        {
            return Ok(new { success = true, redirectUrl = Url.Action("Login", "Auth") });
        }
        return RedirectToAction("Login", "Auth");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword([FromForm] ForgotPasswordViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        
        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            if (isAjax)
            {
                return BadRequest(new { success = false, errors = errs });
            }
            foreach (var err in errs)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(vm);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();

        string BuildResetLink(string token) =>
            Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme)!;

        var (success, errors) = await _auth.RequestPasswordResetAsync(vm, ip, ua, BuildResetLink, ct);

        // Always show success message to prevent user enumeration
        var message = "If the email exists in our system, a password reset link has been sent.";
        
        if (isAjax)
        {
            return Ok(new { success = true, message });
        }

        TempData["ForgotSuccess"] = message;
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword([FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["ResetError"] = "Invalid or missing reset token.";
        }
        return View(new ResetPasswordViewModel { Token = token ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordViewModel vm, CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!ModelState.IsValid)
        {
            var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            if (isAjax)
            {
                return BadRequest(new { success = false, errors = errs });
            }
            foreach (var err in errs)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(vm);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var (success, errors) = await _auth.ResetPasswordAsync(vm, ip, ua, ct);

        if (!success)
        {
            if (isAjax)
            {
                return BadRequest(new { success = false, errors });
            }
            foreach (var err in errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(vm);
        }

        if (isAjax)
        {
            return Ok(new { success = true, redirectUrl = Url.Action("Login", "Auth") });
        }
        
        TempData["ResetSuccess"] = "Password has been reset successfully. Please login with your new password.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();

        if (Guid.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
        {
            await _auth.LogLogoutAsync(userId, ip, ua, ct);
        }

        HttpContext.Session.Clear();

        if (isAjax)
        {
            return Ok(new { success = true, redirectUrl = Url.Action("Index", "Home") });
        }
        return RedirectToAction("Index", "Home");
    }
}