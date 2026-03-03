using IssueManagement.Application.Abstractions;
using IssueManagement.Application.Interfaces;
using IssueManagement.Domain.Repositories;
using IssueManagement.Infrastructure.Authorization;
using IssueManagement.Infrastructure.Options;
using IssueManagement.Infrastructure.Persistence;
using IssueManagement.Infrastructure.Repositories;
using IssueManagement.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Polly;

namespace IssueManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler(options =>
        {
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.Delay = TimeSpan.FromSeconds(3);
            options.Retry.MaxRetryAttempts = 2;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
        }));

        // Options DI (must be registered first so other services can resolve them)
        services.AddOptions(configuration);

        // Database Context DI  
        services.AddDbContext(configuration);

        // Repositories DI
        services.AddRepositories();

        //  Blob Storage DI
        services.AddStorage(configuration);

        // Http Clients DI
        services.AddHttpClients();

        // Authentication DI
        services.AddAuth();

        return services;
    }

    private static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IUnitOfWork, IssueDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(IssueDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
                    //, IWebHostEnvironment web
                    //if (web.IsDevelopment())
                    //{
                    //    options.EnableSensitiveDataLogging();
                    //}
                }));
        return services;
    }
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IIssueRepository, IssueRepository>();
        return services;
    }
    private static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMinioClient>(sp =>
        {
            var minIOOptions = sp.GetRequiredService<IOptions<MinIOOptions>>().Value;
            return new MinioClient()
                .WithEndpoint(minIOOptions.Endpoint, minIOOptions.Port)
                .WithCredentials(minIOOptions.AccessKey, minIOOptions.SecretKey)
                .WithSSL(minIOOptions.UseSSL)
                .Build();
        });
        services.AddScoped<IBlobStorageService, MinioBlobStorageService>();
        return services;
    }

    private static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("APSTokenEndPointClient", (sp, client) =>
        {
            var apsAuthUri = sp.GetRequiredService<IOptions<ApsOptions>>().Value.AuthURI;
            client.Timeout = TimeSpan.FromSeconds(60);
            client.BaseAddress = new Uri(apsAuthUri);
        });
        return services;
    }
    private static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddScoped<IAccessTokenService, ApsAuthService>();
        return services;
    }
    private static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    { 
        services.Configure<MinIOOptions>(configuration.GetSection(MinIOOptions.MinIO));
        services.Configure<ApsOptions>(configuration.GetSection(ApsOptions.APS));
        return services;
    }
}