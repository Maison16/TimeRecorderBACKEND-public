using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
    }
}