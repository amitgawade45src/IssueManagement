using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IssueManagement.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public string ModelUrn { get; private set; } = default!;
        public string UserRole { get; private set; } = "Viewer";

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
            ModelUrn = _configuration["APS:ModelUrn"]
                ?? throw new InvalidOperationException("APS:ModelUrn is not configured.");

            UserRole = User.FindFirstValue(ClaimTypes.Role) ?? "Viewer";
        }
    }
}
