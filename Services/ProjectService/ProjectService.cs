using Microsoft.EntityFrameworkCore;
using System;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;
using TimeRecorderBACKEND.Models;

namespace TimeRecorderBACKEND.Services
{
    public class ProjectService : IProjectService
    {
        private readonly WorkTimeDbContext _context;

        public ProjectService(WorkTimeDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProjectDto>> GetAllAsync()
        {
            return await _context.Projects
                .Where(p => p.ExistenceStatus == ExistenceStatus.Exist)
                .Select(p => ToDto(p))
                .ToListAsync();
        }

        public async Task<ProjectDto?> GetByIdAsync(int id)
        {
            Project? project = await _context.Projects
                .Where(p => p.Id == id && p.ExistenceStatus == ExistenceStatus.Exist)
                .FirstOrDefaultAsync();

            return project == null ? null : ToDto(project);
        }

        public async Task<ProjectDto> CreateAsync(ProjectDto dto)
        {

            Project project = new Project
            {
                Name = dto.Name,
                Description = dto.Description
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return ToDto(project);
        }

        public async Task<ProjectDto?> UpdateAsync(int id, ProjectDto dto)
        {
            Project? project = await _context.Projects
                .Where(p => p.Id == id && p.ExistenceStatus == ExistenceStatus.Exist)
                .FirstOrDefaultAsync();
            if (project == null)
            {
                return null;
            }
            project.Name = dto.Name;
            project.Description = dto.Description;
            await _context.SaveChangesAsync();
            return ToDto(project);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            Project? project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return false;
            }
            project.ExistenceStatus = ExistenceStatus.Deleted;
            await _context.SaveChangesAsync();
            return true;
        }

        private static ProjectDto ToDto(Project p) 
        {
            return new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            };
        }
    }
}
