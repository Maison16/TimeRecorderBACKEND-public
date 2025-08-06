using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Security.Claims;
using System.Threading.RateLimiting;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Models;
using TimeRecorderBACKEND.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using ZiggyCreatures.Caching.Fusion;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Http;
using Swashbuckle.AspNetCore.Annotations;
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // SQL Server register 
        builder.Services.AddDbContext<WorkTimeDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("TimeRecorderConnection")));

        // JWT Bearer Authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Test log
                    Console.WriteLine($"❌ Token rejected: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    //Read token from cookie
                    string? accessToken = context.Request.Cookies["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
            };

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = "name",
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        "https://login.microsoftonline.com/c2a90a0c-eea6-43ab-acf1-bab1bec0c26e/v2.0/.well-known/openid-configuration",
                        new OpenIdConnectConfigurationRetriever());
                    var config = configManager.GetConfigurationAsync().Result;
                    return config.SigningKeys;
                },
                ClockSkew = TimeSpan.FromMinutes(5) // Allow a 5 minute clock skew for token validation
            };
        });
        Polly.Timeout.AsyncTimeoutPolicy<HttpResponseMessage> timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
        Polly.Retry.AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        Polly.CircuitBreaker.AsyncCircuitBreakerPolicy<HttpResponseMessage> circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        builder.Services.AddHttpClient("AiClient")
            .AddHttpMessageHandler(() => new PolicyHttpMessageHandler(timeoutPolicy))
            .AddHttpMessageHandler(() => new PolicyHttpMessageHandler(retryPolicy))
            .AddHttpMessageHandler(() => new PolicyHttpMessageHandler(circuitBreakerPolicy));
        builder.Services.AddFusionCache();
        builder.Services.AddAuthorization();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:5173", "https://deep-huge-pika.ngrok-free.app", "https://localhost:7023", "https://localhost:5173", "https://yellow-moss-04f827803.2.azurestaticapps.net")
                      .AllowCredentials()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Controllers
        builder.Services.AddValidatorsFromAssemblyContaining<RequestDayOffValidator>();
        builder.Services.AddControllers().AddFluentValidation();

        // Swagger Endpoints
        builder.Services.AddEndpointsApiExplorer();

        //  Swagger configuration
        builder.Services.AddSwaggerGen(options =>
        {
            options.EnableAnnotations();
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "TimeRecorder API", Version = "v1" });
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);


            // OAuth2 Security Definition
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("https://login.microsoftonline.com/c2a90a0c-eea6-43ab-acf1-bab1bec0c26e/oauth2/v2.0/authorize"),
                        TokenUrl = new Uri("https://login.microsoftonline.com/c2a90a0c-eea6-43ab-acf1-bab1bec0c26e/oauth2/v2.0/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            { "api://8b8a49ef-3242-4695-985d-9a7eb39071ae/TimeRecorderBACKEND.all", "Access API TimeRecorderBACKEND" }
                        }
                    }
                }
            });
            // Auth in Swagger
            options.OperationFilter<SecurityRequirementsOperationFilter>();
        });

        // Microsoft Graph with Client Secret Credential (Azure AD)
        builder.Services.AddSingleton<GraphServiceClient>(sp =>
        {
            string? tenantId = builder.Configuration["AzureAd:TenantId"];
            string? clientId = builder.Configuration["AzureAd:ClientId"];
            string? clientSecret = builder.Configuration["AzureAd:ClientSecret"];

            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            return new GraphServiceClient(clientSecretCredential);
        });

        // BaclGround services
        builder.Services.AddHostedService<WorkLogBackgroundService>();
        builder.Services.AddHostedService<DayOffBackgroundService>();
        builder.Services.AddHostedService<TeamsProactiveThreadService>();
        builder.Services.AddHostedService<TeamsPresenceBackgroundService>();
        string? emailFrom = builder.Configuration["Email:From"];
        string? emailPassword = builder.Configuration["Email:Password"];
        if (string.IsNullOrWhiteSpace(emailFrom) || string.IsNullOrWhiteSpace(emailPassword))
        {
            throw new InvalidOperationException("Secrets Email:From or Email:Password wasn't set");
        }

        // Email register
        builder.Services.AddSingleton<IEmailService>(sp =>
            new EmailService(emailFrom, emailPassword)
        );

        // (DI)
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<IDayOffService, DayOffService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IWorkLogService, WorkLogService>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<ISummaryService, SummaryService>();
        builder.Services.AddScoped<ITeamsService, TeamsService>();
        builder.Services.AddScoped<ISettingsService, SettingsService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddHttpContextAccessor();
        //add rate timiter
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("UserIdPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetUserIdOrIp(context),
                    factory: key => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 150,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }
                )
            );
            options.RejectionStatusCode = 429;
        });
        builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(sp =>
        {
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            ILogger<CloudAdapter> logger = sp.GetRequiredService<ILogger<CloudAdapter>>();
            BotFrameworkAuthentication botFrameworkAuthentication = sp.GetRequiredService<BotFrameworkAuthentication>();

            return new CloudAdapter(botFrameworkAuthentication, logger);
        });
        builder.Services.AddSingleton<BotFrameworkAuthentication>(sp =>
            new ConfigurationBotFrameworkAuthentication(builder.Configuration));
        builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
        builder.Services.AddTransient<IBot, TeamsBot>();
        builder.Services.AddSignalR();
        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            // Use Swagger only in development
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TimeRecorder API V1");
                c.OAuthClientId(builder.Configuration["AzureAd:SwaggerId"]);
                c.OAuthUsePkce();
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string> { { "prompt", "select_account" } });
            });
        }

        //  HTTP to HTTPS - turned off cause Ngrok 
        //app.UseHttpsRedirection();
        app.UseCors("AllowFrontend");

        // Middleware to add access token to requests
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/messages"))
            {
                await next();
                return;
            }
            if (context.Request.Path.StartsWithSegments("/workstatushub"))
            {
                string? token = context.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Request.Headers["Authorization"] = $"Bearer {token}";
                }
            }
            else
            {
                string? token = context.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Request.Headers["Authorization"] = $"Bearer {token}";
                }
            }
            await next();

        });

        // Authentication and Authorization
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        //Map Controllers
        app.MapControllers().RequireRateLimiting("UserIdPolicy");

        // SignalR StatusHub
        app.MapHub<WorkStatusHub>("/workstatushub");
        // Setting default Settings
        using (IServiceScope scope = app.Services.CreateScope())
        {
            WorkTimeDbContext dbContext = scope.ServiceProvider.GetRequiredService<WorkTimeDbContext>();
            if (!dbContext.Settings.Any())
            {
                dbContext.Settings.Add(new Settings());
                dbContext.SaveChanges();
            }
            if (!dbContext.LastWorkOnDayMassageDate.Any())
            {
                dbContext.LastWorkOnDayMassageDate.Add(new LastWorkOnDayMassageDate
                {
                    LastMessageDate = DateTime.Now.AddDays(-1)
                });
                dbContext.SaveChanges();
            }
            // Run
            app.Run();
        }
        static string GetUserIdOrIp(HttpContext context)
        {
            string? userId = context.User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            Console.WriteLine(userId);
            if (!string.IsNullOrEmpty(userId))
                return userId;

            return context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        }
    }
}