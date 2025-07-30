using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Diagnostics;
using System.Security.Claims;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;
using TimeRecorderBACKEND.Models;
using ZiggyCreatures.Caching.Fusion;

namespace TimeRecorderBACKEND.Services
{
    public class UserService : IUserService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly WorkTimeDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFusionCache _cache;
        private ClaimsPrincipal? user => _httpContextAccessor.HttpContext?.User;

        public UserService(GraphServiceClient graphClient, WorkTimeDbContext context, IEmailService emailService, IHttpContextAccessor httpContextAccessor, IFusionCache cache)
        {
            _graphClient = graphClient;
            _context = context;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }


        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            return await _cache.GetOrSetAsync(
                "users_all",
                async _ =>
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Fetching users from database...");
                    Console.ResetColor();
                    List<Models.User> users = await _context.Users.Where(u => u.ExistenceStatus == ExistenceStatus.Exist).ToListAsync();
                    return users.Select(ToDto).ToList();
                },
                TimeSpan.FromHours(24)
            );
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            Models.User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.ExistenceStatus == ExistenceStatus.Exist);
            return user == null ? null : ToDto(user);
        }

        public async Task<bool> SyncUsersAsync()
        {
            // Pobierz stan lokalnej bazy
            List<Models.User> localUsers = await _context.Users.ToListAsync();
            Dictionary<Guid, Models.User> localUsersDict = localUsers.ToDictionary(u => u.Id, u => u);

            Dictionary<Guid, Microsoft.Graph.Models.User> usersFromGraph = new Dictionary<Guid, Microsoft.Graph.Models.User>();

            // Delta query
            Microsoft.Graph.Users.Delta.DeltaResponse? deltaPage = await _graphClient.Users.Delta.GetAsync(config =>
            {
                config.QueryParameters.Select = new[] { "id", "givenName", "surname", "mail", "userPrincipalName" };
                config.QueryParameters.Top = 100;
            });

            while (deltaPage?.Value != null)
            {
                foreach (Microsoft.Graph.Models.User graphUser in deltaPage.Value)
                {
                    if (!Guid.TryParse(graphUser.Id, out Guid userId))
                    {
                        Debug.WriteLine($"Niepoprawny GUID: {graphUser.Id}");
                        continue;
                    }

                    if (graphUser.AdditionalData != null && graphUser.AdditionalData.ContainsKey("@removed"))
                    {
                        // User was deleted in Azur
                        Models.User? localUserToDelete = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == userId && u.ExistenceStatus == ExistenceStatus.Exist);

                        if (localUserToDelete != null)
                        {
                            localUserToDelete.ExistenceStatus = ExistenceStatus.Deleted;

                            _context.Users.Update(localUserToDelete);
                            localUsersDict.Remove(userId);
                        }
                        continue;
                    }

                    // Get data
                    string? email = graphUser.Mail;
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        email = graphUser.UserPrincipalName;
                    }

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        continue;
                    }

                    string name = graphUser.GivenName ?? "";
                    string surname = graphUser.Surname ?? "";

                    if (localUsersDict.TryGetValue(userId, out Models.User? existingUser))
                    {
                        // Update if diff
                        if (existingUser.Name != name || existingUser.Surname != surname || existingUser.Email != email)
                        {
                            existingUser.Name = name;
                            existingUser.Surname = surname;
                            existingUser.Email = email;
                            _context.Users.Update(existingUser);
                        }
                        if (existingUser.ExistenceStatus == ExistenceStatus.Deleted)
                        {
                            existingUser.ExistenceStatus = ExistenceStatus.Exist;
                        }

                    }
                    else
                    { 
                            // Add totaly new user
                            Models.User newUser = new Models.User
                            {
                                Id = userId,
                                Name = name,
                                Surname = surname,
                                Email = email,
                                ExistenceStatus = ExistenceStatus.Exist
                            };

                            await _context.Users.AddAsync(newUser);

                            _emailService.SendAsync(
                                email,
                                $"Welcome to TimeRecorder APP {name} {surname}!",
                                "You have been added to the TimeRecorder system. You are welcome! :D"
                            );
                        
                    }

                    usersFromGraph[userId] = graphUser;
                }

                if (string.IsNullOrWhiteSpace(deltaPage.OdataNextLink))
                    break;

                deltaPage = await _graphClient.Users.Delta.WithUrl(deltaPage.OdataNextLink).GetAsync();
            }

            await _context.SaveChangesAsync();
            await _cache.RemoveAsync("users_all");
            return true;
        }
        public UserInfoDto GetUserProfile()
        {
            if (user == null)
            {
                return new UserInfoDto { IsAuthenticated = false };
            }
            string? userId = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (userId == null)
            {
                return null;
            }
            string? email = user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value
                           ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

            string? givenName = user.FindFirst(ClaimTypes.GivenName)?.Value;
            string? familyName = user.FindFirst(ClaimTypes.Surname)?.Value;
            var roles = user.FindAll("roles").Select(c => c.Value).ToList();
            if (!roles.Any())
            {
                roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            }
            Guid Id;
                if (!Guid.TryParse(userId, out Id))
                {
                    Id = Guid.Empty;
                }

            return new UserInfoDto
            {
                Email = email,
                Name = givenName,
                Surname = familyName,
                Roles = roles,
                IsAuthenticated = true
            };
        }

        public async Task<bool> AssignProjectAsync(Guid userId, int projectId)
        {
            Models.User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            Project? project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
            {
                return false;
            }

            user.Project = project;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnassignProjectAsync(Guid userId)
        {
            Models.User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            user.Project = null;
            user.ProjectId = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ProjectDto?> GetUserProjectAsync(Guid userId)
        {
            Models.User? user = await _context.Users
                .Include(u => u.Project)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.Project == null)
            {
                return null;
            }

            return new ProjectDto
            {
                Id = user.Project.Id,
                Name = user.Project.Name,
                Description = user.Project.Description
            };
        }

        private static UserDto ToDto(Models.User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email
            };
        }
    }
}
