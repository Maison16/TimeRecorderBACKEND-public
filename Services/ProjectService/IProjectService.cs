using TimeRecorderBACKEND.Dtos;

namespace TimeRecorderBACKEND.Services
{
    public interface IProjectService
    {
        Task<IEnumerable<ProjectDto>> GetAllAsync();
        Task<ProjectDto?> GetByIdAsync(int id);
        Task<ProjectDto> CreateAsync(ProjectDto dto);
        Task<ProjectDto?> UpdateAsync(int id, ProjectDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
