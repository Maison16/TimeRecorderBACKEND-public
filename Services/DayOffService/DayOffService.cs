using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using System;
using System.Data;
using System.Security.Claims;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;
using TimeRecorderBACKEND.Models;

namespace TimeRecorderBACKEND.Services
{
    public class DayOffService : IDayOffService
    {
        private readonly WorkTimeDbContext _context;

        public DayOffService(WorkTimeDbContext context)
        {
            _context = context;
        }

        public async Task<DayOffRequestDto> RequestDayOffAsync(Guid userId, DateTime startDate, DateTime endDate, string? reason)
        {
            if (endDate < startDate)
            {
                throw new ArgumentException("End date cannot be before start date.");
            } 

            bool hasSamePart = await _context.DayOffRequests
                .AnyAsync(r =>
                    r.UserId == userId &&
                    r.DateStart <= endDate.Date &&
                    r.DateEnd >= startDate.Date &&
                    r.Status != DayOffStatus.Cancelled &&
                    r.ExistenceStatus == ExistenceStatus.Exist

                );

            if (hasSamePart)
            {
                throw new InvalidOperationException("The selected date range overlaps with an existing day-off request.");
            }

            DayOffRequest request = new DayOffRequest
            {
                UserId = userId,
                DateStart = startDate.Date,
                DateEnd = endDate.Date,
                Reason = reason
            };

            _context.DayOffRequests.Add(request);
            await _context.SaveChangesAsync();

            return ToDto(request);
        }


        public async Task<DayOffRequestDto> ChangeDayOffStatusAsync(int requestId, DayOffStatus status, ClaimsPrincipal user)
        {
            DayOffRequest? request = await _context.DayOffRequests.FindAsync(requestId);
            if (request == null)
            {
                throw new Exception("DayOffRequest doesn't exist");
            }

            // Testing role and status
            if (status == DayOffStatus.Cancelled)
            {
                string? userId = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                if (request.UserId.ToString() != userId)
                {
                    throw new   ("You can cancel only your DayOffRequest.");
                }
            }
            else
            {
                // Only admins can approve or reject requests
                if (!user.IsInRole("Admin"))
                {
                    throw new UnauthorizedAccessException("Only admin can Approve or Reject DayOffRequests.");
                }
            }

            request.Status = status;
            await _context.SaveChangesAsync();
            return ToDto(request);
        }

        public async Task<IEnumerable<DayOffRequestDto>> GetUserDayOffsAsync(Guid userId)
        {
            return await _context.DayOffRequests
                .Where(x => x.UserId == userId && x.ExistenceStatus == ExistenceStatus.Exist)
                .Select(x => ToDto(x))
                .ToListAsync();
        }
        public async Task DeleteDayOffRequestAsync(int requestId)
        {
            DayOffRequest? request = await _context.DayOffRequests.FindAsync(requestId);
            if (request == null)
            {
                throw new Exception("DayOffRequest doesn't exist");
            }
            request.ExistenceStatus = ExistenceStatus.Deleted;
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<DayOffRequestDtoWithUserNameAndSurname>> Filter(
        Guid? userId = null,
        string? name = null,
        string? surname = null,
        DayOffStatus[]? statuses = null,
        DateTime? dateStart = null,
        DateTime? dateEnd = null,
        bool? isDeleted = false,
        int? pageNumber = null,
        int? pageSize = null
        )
        {
            IQueryable<DayOffRequest> query;
            if (isDeleted == true)
            {
                query = _context.DayOffRequests
                .Where(x => x.ExistenceStatus == ExistenceStatus.Deleted);
            }
            else
            {
                query = _context.DayOffRequests
                    .Where(x => x.ExistenceStatus == ExistenceStatus.Exist);
            }

            if (userId.HasValue)
            {
                query = query.Where(x => x.UserId == userId.Value);
            }
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(x => x.User.Name.Contains(name));
            }
            if (!string.IsNullOrEmpty(surname))
            {
                query = query.Where(x => x.User.Surname.Contains(surname));
            }
            if (statuses != null && statuses.Length > 0)
            {
                query = query.Where(r => statuses.Contains(r.Status));
            }
            if (dateStart.HasValue)
            {
                query = query.Where(x => x.DateStart >= dateStart.Value.Date);
            }
            if (dateEnd.HasValue)
            {
                query = query.Where(x => x.DateEnd <= dateEnd.Value.Date);
            }
            query = query.OrderByDescending(x => x.DateStart);

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                query = query
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value);
            }
            List<DayOffRequest> results = await query.Include(x => x.User).ToListAsync(); 
            return results.Select(x => new DayOffRequestDtoWithUserNameAndSurname
            {
                Id = x.Id,
                DateStart = x.DateStart.Date,
                DateEnd = x.DateEnd.Date,
                Status = x.Status,
                Reason = x.Reason,
                UserId = x.UserId,
                UserName = x.User.Name,
                UserSurname = x.User.Surname
            }).ToList();
        }
        public async Task<DayOffRequestDto> GetDayOffRequestByIdAsync(int requestId)
        {
            DayOffRequest? entity = await _context.DayOffRequests
                .Where(x => x.Id == requestId && x.ExistenceStatus == ExistenceStatus.Exist)
                .Include(x => x.User)
                .FirstOrDefaultAsync();

            return entity == null ? null : ToDto(entity);
        }

        public async Task<DayOffRequestDto> EditDayOffRequestAsync(int requestId, DateTime newStartDate, DateTime newEndDate, string? newReason, ClaimsPrincipal user)
        {
            if (newEndDate < newStartDate)
            {
                throw new ArgumentException("End date cannot be before start date.");
            }

            DayOffRequest? request = await _context.DayOffRequests.FindAsync(requestId);
            if (request == null)
            {
                throw new Exception("DayOffRequest doesn't exist");
            }

            if (!user.IsInRole("Admin"))
            {
                string? userId = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                if (request.UserId.ToString() != userId)
                {
                    throw new UnauthorizedAccessException("You can only edit your own DayOffRequests.");
                }
            }

            bool hasSamePart = await _context.DayOffRequests
                .AnyAsync(r =>
                    r.Id != request.Id &&
                    r.UserId == request.UserId &&
                    r.DateStart <= newEndDate.Date &&
                    r.DateEnd >= newStartDate.Date &&
                    r.Status != DayOffStatus.Cancelled &&
                    r.ExistenceStatus == ExistenceStatus.Exist);

            if (hasSamePart)
            {
                throw new InvalidOperationException("The new date range overlaps with an existing day-off request.");
            }

            request.DateStart = newStartDate.Date;
            request.DateEnd = newEndDate.Date;
            request.Reason = newReason;
            request.Status = DayOffStatus.Pending; 

            await _context.SaveChangesAsync();
            return ToDto(request);
        }

        public async Task<DayOffRequestDto> RestoreDayOffRequestAsync(int requestId)
        {
            DayOffRequest? request = await _context.DayOffRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ExistenceStatus == ExistenceStatus.Deleted);

            if (request == null)
            {
                throw new Exception("DayOffRequest doesn't exist or is not deleted.");
            }

            request.ExistenceStatus = ExistenceStatus.Exist;
            await _context.SaveChangesAsync();

            return ToDto(request);
        }

        private static DayOffRequestDto ToDto(DayOffRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "DayOffRequest cannot be null");
            }
            if (request.UserId == null)
            {
                throw new InvalidOperationException("UserId cannot be null in DayOffRequest");
            }

            return new DayOffRequestDto
            {
                Id = request.Id,
                DateStart = request.DateStart.Date,
                DateEnd = request.DateEnd.Date,
                Status = request.Status,
                Reason = request.Reason,
                UserId = request.UserId
            };
        }
    }
}
