using System.Security.Claims;
using TimeRecorderBACKEND.Dtos;

namespace TimeRecorderBACKEND.Services
{
    public interface IAuthService
    {
        bool ValidateToken(string token, out ClaimsPrincipal? principal);
        void SetAuthCookie(HttpResponse response, string token);
        void RemoveAuthCookie(HttpResponse response);
        UserInfoDto GetUserInfo(ClaimsPrincipal user);
    }
}