using IssueManagement.Application.Abstractions;
using IssueManagement.Application.Interfaces;
using IssueManagement.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace IssueManagement.Application.UseCases.Issues.Queries;

internal sealed class GetIssueTokenQueryHandler(IAccessTokenService _accessTokenService, ILogger<GetIssueTokenQueryHandler> _logger) : IQueryHandler<GetIssueTokenQuery, (string Token, int ExpiresIn)>
{
    public async Task<Result<(string Token, int ExpiresIn)>> Handle(GetIssueTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (token, expiresIn) = await _accessTokenService.GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to retrieve access token. at {dateTime}", DateTime.UtcNow);
                return Result.Failure<(string Token, int ExpiresIn)>(new Error("500", "Failed to retrieve access token."));
            }
            return Result.Success((token, expiresIn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token at {dateTime}", DateTime.UtcNow);
            return Result.Failure<(string Token, int ExpiresIn)>(new Error("500", "An error occurred while retrieving the access token"));
        }
    }
}