using IssueManagement.Application;
using IssueManagement.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


// Configure Serilog from appsettings.json
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration); 
builder.Services.AddHealthChecks();
 
// „Ÿ„Ÿ Cookie Authentication „Ÿ„Ÿ
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Return 401/403 for API requests instead of redirecting to the login page
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Admin: full access (delete issues, manage everything)
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Manager+: create issues, change status, upload photos
    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Viewer+: read-only access (any authenticated user)
    options.AddPolicy("ViewerOrAbove", policy =>
        policy.RequireRole("Admin", "Manager", "Viewer"));
});

// „Ÿ„Ÿ Swagger / OpenAPI „Ÿ„Ÿ
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BIM Issue Manager API",
        Version = "v1",
        Description = "REST API for managing BIM issues, photos, and status workflows."
    });

    // Include XML comments for richer Swagger descriptions
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// „Ÿ„Ÿ Swagger middleware (development only) „Ÿ„Ÿ
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BIM Issue Manager API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add Serilog request logging middleware
app.UseSerilogRequestLogging();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/healthcheck");

app.MapRazorPages();
app.MapControllers();

app.Run();
 
public partial class Program;