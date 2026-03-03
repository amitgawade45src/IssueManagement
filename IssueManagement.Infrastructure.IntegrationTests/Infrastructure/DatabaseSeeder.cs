using IssueManagement.Infrastructure.DTO.DBSets;
using IssueManagement.Infrastructure.Persistence;
using System.Text.Json;

namespace IssueManagement.Infrastructure.IntegrationTests.Infrastructure;

public static class DatabaseSeeder
{
    private static readonly string _databaseDataBasePath = ".files";

    public static void SeedBaseData(IssueDbContext context)
    {
        SeedIssueData(context, $"{_databaseDataBasePath}/IssueData.json");
    }
    private static void SeedIssueData(IssueDbContext context, string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var issues = JsonSerializer.Deserialize<List<IssueModel>>(json);
        context.Issues.AddRange(issues!);
        context.SaveChanges();
    }
}
