using IssueManagement.Application.Interfaces;
using IssueManagement.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json; // faster than newtonsoft

namespace IssueManagement.Infrastructure.Authorization;

internal sealed class ApsAuthService(IHttpClientFactory _httpClientFactory, IOptions<ApsOptions> _options, ILogger<ApsAuthService> _logger) : IAccessTokenService
{
    private readonly ApsOptions apsOptions = _options.Value;
    public async Task<(string AccessToken, int ExpiresIn)> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = apsOptions.ClientId,
            ["client_secret"] = apsOptions.ClientSecret,
            ["grant_type"] = apsOptions.GrantType,
            ["scope"] = apsOptions.Scope
        });
        _logger.LogInformation("Requesting APS access token from {AuthURI} with client_id {ClientId}", apsOptions.AuthURI, apsOptions.ClientId);

        using (var client = _httpClientFactory.CreateClient("APSTokenEndPointClient"))
        {
            _logger.LogInformation("Created HTTP client for APS token request.");
            var response = await client.PostAsync(apsOptions.AuthURI, requestBody, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to obtain APS access token. Status Code: {StatusCode}, Reason: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                return (string.Empty, 0);
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<ApsTokenResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Failed to deserialize APS token response.");
            return (tokenResponse.AccessToken, tokenResponse.ExpiresIn);
        }
    }
}
