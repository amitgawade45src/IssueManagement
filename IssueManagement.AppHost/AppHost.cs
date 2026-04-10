var builder = DistributedApplication.CreateBuilder(args);

 
var apiService = builder.AddProject<Projects.IssueManagement>("issuemanagement");


builder.AddProject<Projects.IssueManagement>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);


await builder.Build().RunAsync();
