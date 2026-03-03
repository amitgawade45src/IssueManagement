namespace IssueManagement.Application.Interfaces;

public interface IAccessTokenService
{ 
    Task<(string AccessToken, int ExpiresIn)> GetAccessTokenAsync(CancellationToken cancellationToken = default); // Gets a 2-legged OAuth access token for APS (viewables:read scope).
}  
