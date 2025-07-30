using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TimeRecorderBACKEND.Dtos;
namespace TimeRecorderBACKEND.Services
{
    public class AuthService : IAuthService
    {
        private const string Issuer = "https://sts.windows.net/c2a90a0c-eea6-43ab-acf1-bab1bec0c26e/";
        private const string Audience = "api://8b8a49ef-3242-4695-985d-9a7eb39071ae";
        private const string OpenIdConfigUrl = "https://login.microsoftonline.com/c2a90a0c-eea6-43ab-acf1-bab1bec0c26e/v2.0/.well-known/openid-configuration";

        public bool ValidateToken(string token, out ClaimsPrincipal? principal)
        {
            principal = null;
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                TokenValidationParameters validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true, 
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = "name",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    {
                        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                            OpenIdConfigUrl,
                            new OpenIdConnectConfigurationRetriever());
                        OpenIdConnectConfiguration config = configManager.GetConfigurationAsync().Result;
                        return config.SigningKeys;
                    },
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetAuthCookie(HttpResponse response, string token)
        {
            response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.Now.AddMinutes(30)
            });
        }

        public void RemoveAuthCookie(HttpResponse response)
        {
            response.Cookies.Append("access_token", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.Now.AddDays(-1)
            });
        }

        public UserInfoDto GetUserInfo(ClaimsPrincipal user)
        {
            List<string> roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
                .Select(c => c.Value)
                .ToList();

            string? userId = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? email = user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value;
            string? name = user.FindFirst(ClaimTypes.GivenName)?.Value
                ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
            string? surname = user.FindFirst(ClaimTypes.Surname)?.Value;

            return new UserInfoDto
            {
                Id = userId,
                Email = email,
                Name = name,
                Surname = surname,
                IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
                Roles = roles
            };
        }
    }
}