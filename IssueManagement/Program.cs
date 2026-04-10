using IssueManagement.Application;
using IssueManagement.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi; 
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();


// Configure Serilog from appsettings.json
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

// ── Cookie Authentication ──
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

// ── Swagger / OpenAPI ──
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

app.MapDefaultEndpoints();

// ── Apply pending EF Core migrations on startup ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IssueManagement.Infrastructure.Persistence.IssueDbContext>();
    var retries = 10;
    for (var i = 0; i < retries; i++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (i < retries - 1)
        {
            app.Logger.LogWarning(ex, "Database not ready, retrying in 3 seconds... ({Attempt}/{MaxRetries})", i + 1, retries);
            await Task.Delay(3000);
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ── Swagger middleware (development only) ──
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

await app.RunAsync();

public partial class Program;