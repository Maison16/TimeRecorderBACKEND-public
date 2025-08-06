using System.Security.Claims;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Models;

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
        Task<IEnumerable<UserDtoWithProject>> GetAllUsersWithProjectsAsync(
            int? pageNumber = null,
            int? pageSize = null,
            string? search = null,
            bool onlyWithoutProject = false);
        Task<IEnumerable<UserDto>> GetUsersByProjectAsync(int projectId);
    }
}
