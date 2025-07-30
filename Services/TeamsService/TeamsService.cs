using Azure.Core;
using Azure.Identity;
using Microsoft.Bot.Builder; 
using Microsoft.Bot.Builder.Integration.AspNet.Core; 
using Microsoft.Bot.Schema; 
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using TimeRecorderBACKEND.Services;

public class TeamsService : ITeamsService
{
    private readonly GraphServiceClient _graphClient;
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IConfiguration _configuration; 
    private readonly string _botAppId; 
    private readonly string? tenantId;
    public TeamsService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, GraphServiceClient? graphClient = null, IBotFrameworkHttpAdapter adapter = null)
    {
        _configuration = configuration;
        _adapter = adapter;
        _botAppId = _configuration["Teams:BotAppId"] ?? throw new InvalidOperationException("Teams:BotAppId is missing in configuration.");
        tenantId = configuration["AzureAd:TenantId"];
        if (graphClient != null)
        {
            _graphClient = graphClient;
            return;
        }

        string? accessToken = httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
                                .ToString().Replace("Bearer ", "");

        if (!string.IsNullOrEmpty(accessToken))
        {
            DefaultAzureCredential tokenCredential = new DefaultAzureCredential();
            _graphClient = new GraphServiceClient(tokenCredential);
        }
        // Fallback
        else
        {
            string? clientId = configuration["AzureAd:ClientId"];
            string? clientSecret = configuration["AzureAd:ClientSecret"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Missing required AzureAd configuration for ClientSecretCredential.");
            }

            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _graphClient = new GraphServiceClient(clientSecretCredential);
        }
    }

    public async Task SendPrivateMessageAsync(string userAadId, string message)
    {
            if (_adapter == null)
            {
                throw new InvalidOperationException("Adapter is not initialized.");
            }
        try
        {
            string serviceUrl = "https://smba.trafficmanager.net/emea/";
            string channelId = "msteams";
            ConversationParameters parameters = new ConversationParameters
            {
                IsGroup = false,
                Bot = new ChannelAccount(id: _botAppId),
                Members = new List<ChannelAccount> { new ChannelAccount(id: userAadId) },
                TenantId = tenantId,
                ChannelData = new
                {
                    tenant = new
                    {
                        id = tenantId
                    }
                }

            };
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine($"SendPrivateMessageAsync: userAadId={userAadId}, message={message}, botAppId={_botAppId}, tenantId={tenantId}");
            Console.ResetColor();

            await ((CloudAdapterBase)_adapter).CreateConversationAsync(
                    _botAppId,
                    channelId,
                    serviceUrl,
                    null,
                    parameters,
                    async (turnContext, cancellationToken) =>
                    {
                        var activity = MessageFactory.Text(message);
                        await turnContext.SendActivityAsync(activity, cancellationToken);
                    }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending private message to {userAadId}: {ex.Message}");
        }
    }
}
