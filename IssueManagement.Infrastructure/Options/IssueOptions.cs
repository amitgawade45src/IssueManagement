namespace IssueManagement.Infrastructure.Options;

internal sealed record MinIOOptions
{
    public const string MinIO = nameof(MinIO);
    public required string Endpoint { get; init; }
    public int Port { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string BucketName { get; init; } 
    public int ExpiryInSeconds { get; init; }
    public bool UseSSL { get; init; }
}  
internal sealed record ApsOptions
{
    public const string APS = nameof(APS);
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string GrantType { get; init; }
    public required string Scope { get; init; }
    public required string ModelUrn { get; init; }
    public required string AuthURI { get; init; }
}