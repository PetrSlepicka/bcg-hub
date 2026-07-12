using System.Security.Claims;
using BcgHub.Api.Application;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth, IAntiforgery antiforgery) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    public ActionResult<object> GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    [HttpPost("login")]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await auth.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        if (user is null) return Unauthorized(new { code = "invalid_credentials", message = "Neplatný e-mail nebo heslo." });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Name, user.FullName) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = true, AllowRefresh = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) });
        return Ok(new CurrentUserDto(user.Id, user.Email, user.FullName));
    }

    [HttpGet("session")]
    public ActionResult<CurrentUserDto> Session() => Ok(CurrentUser());

    [ValidateAntiForgeryToken]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private CurrentUserDto CurrentUser() => new(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.FindFirstValue(ClaimTypes.Email)!, User.FindFirstValue(ClaimTypes.Name)!);
}
