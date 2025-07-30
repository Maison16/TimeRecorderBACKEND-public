using Microsoft.AspNetCore.SignalR;

public class WorkStatusHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var isAuthenticated = Context.User?.Identity?.IsAuthenticated ?? false;
        Console.BackgroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"SignalR connected: {userId}, Authenticated: {isAuthenticated}");
        Console.ResetColor();
        await base.OnConnectedAsync();
    }
}