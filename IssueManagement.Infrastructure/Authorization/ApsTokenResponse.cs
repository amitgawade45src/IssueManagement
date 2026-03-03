using System.Text.Json.Serialization;

namespace IssueManagement.Infrastructure.Authorization;
internal sealed class ApsTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; set; }
}
