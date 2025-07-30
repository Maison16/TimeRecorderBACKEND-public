using System.Security.Claims;
using TimeRecorderBACKEND.Dtos;

namespace TimeRecorderBACKEND.Services
{
    public interface IUserService
    {
        UserInfoDto GetUserProfile();
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<bool> SyncUsersAsync();
        Task<bool> AssignProjectAsync(Guid userId, int projectId);
        Task<bool> UnassignProjectAsync(Guid userId);
        Task<ProjectDto?> GetUserProjectAsync(Guid userId);
    }
}
