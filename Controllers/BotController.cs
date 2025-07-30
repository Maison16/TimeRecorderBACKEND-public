using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

/// <summary>
/// Controller responsible for handling incoming Bot Framework messages.
/// This endpoint is used by Microsoft Teams and other channels to communicate with the bot.
/// </summary>
[Route("api/messages")]
[AllowAnonymous]
[ApiController]
public class BotController : ControllerBase
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IBot _bot;
    /// <summary>
    /// Initializes a new instance of the <see cref="BotController"/> class.
    /// </summary>
    /// <param name="adapter">The Bot Framework HTTP adapter.</param>
    /// <param name="bot">The bot implementation.</param>
    public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
    {
        _adapter = adapter;
        _bot = bot;
    }
    /// <summary>
    /// Receives and processes incoming activities from Bot Framework channels.
    /// </summary>
    /// <returns>An asynchronous operation.</returns>
    [HttpPost]
    public async Task PostAsync()
    {
        await _adapter.ProcessAsync(Request, Response, _bot);
    }
}