using IssueManagement.Infrastructure.Options;
using IssueManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minio;
using Testcontainers.Minio;
using Testcontainers.MsSql;

namespace IssueManagement.Infrastructure.IntegrationTests.Infrastructure;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Admin123!")
        .WithCleanUp(true)
        .Build();

    private readonly MinioContainer _minioContainer = new MinioBuilder("minio/minio:RELEASE.2023-01-31T02-24-19Z")
        .WithUsername("minioadmin")
        .WithPassword("Admin123!")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _minioContainer.StartAsync();

        // Seed database
        var databaseOptions = new DbContextOptionsBuilder<IssueDbContext>()
            .UseSqlServer(_dbContainer.GetConnectionString())
            .Options;

        var dbContext = new IssueDbContext(databaseOptions);
        await dbContext.Database.MigrateAsync();
        DatabaseSeeder.SeedBaseData(dbContext);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // ── Replace SQL Server DbContext ──
            var dbContextDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<IssueDbContext>));
            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor); 

            services.AddDbContext<IssueDbContext>(options =>
                options.UseSqlServer(_dbContainer.GetConnectionString()));

            // ── Replace IMinioClient ──
            var minioDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMinioClient));
            if (minioDescriptor is not null)
                services.Remove(minioDescriptor);

            var minioHost = _minioContainer.Hostname;
            var minioPort = _minioContainer.GetMappedPublicPort(9000);

            services.AddSingleton<IMinioClient>(_ =>
                (IMinioClient)new MinioClient()
                    .WithEndpoint(minioHost, minioPort)
                    .WithCredentials("minioadmin", "Admin123!")
                    .WithSSL(false)
                    .Build());

            // ── Replace MinIOOptions (sealed record with init-only setters) ──
            services.RemoveAll<IOptions<MinIOOptions>>();
            services.RemoveAll<IOptionsSnapshot<MinIOOptions>>();
            services.RemoveAll<IOptionsMonitor<MinIOOptions>>();

            var testMinioOptions = new MinIOOptions
            {
                Endpoint = minioHost,
                Port = minioPort,
                AccessKey = "minioadmin",
                SecretKey = "Admin123!",
                BucketName = "integration-tests",
                ExpiryInSeconds = 3600,
                UseSSL = false
            };
            services.AddSingleton<IOptions<MinIOOptions>>(Microsoft.Extensions.Options.Options.Create(testMinioOptions));
        });
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _minioContainer.StopAsync();
    }
}