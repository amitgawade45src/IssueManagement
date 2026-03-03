using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IssueManagement.Pages.Auth;

public class LoginModel : PageModel
{
    [BindProperty]
    public string DisplayName { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Role { get; set; } = "Admin";

    public string? ReturnUrl { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        // If already authenticated, redirect away
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(returnUrl ?? "/");

        ReturnUrl = returnUrl ?? "/";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= "/";

        if (string.IsNullOrWhiteSpace(DisplayName))
            DisplayName = "Mock User";

        if (string.IsNullOrWhiteSpace(Email))
            Email = "mock@issuemanagement.local";

        if (string.IsNullOrWhiteSpace(Role))
            Role = "Viewer";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, DisplayName),
            new(ClaimTypes.Email, Email),
            new(ClaimTypes.Role, Role),
            new("FullName", DisplayName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return LocalRedirect(returnUrl);
    }
}
