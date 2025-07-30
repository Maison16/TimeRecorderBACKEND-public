using System.Security.Claims;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Services
{
    public interface IDayOffService
    {
        Task<DayOffRequestDto> RequestDayOffAsync(Guid userId, DateTime startDate, DateTime endDate, string? reason);
        Task<DayOffRequestDto> ChangeDayOffStatusAsync(int requestId, DayOffStatus status, ClaimsPrincipal user);
        Task<IEnumerable<DayOffRequestDto>> GetUserDayOffsAsync(Guid userId);
        Task<IEnumerable<DayOffRequestDtoWithUserNameAndSurname>> Filter(
            Guid? userId = null, 
            string? name = null, 
            string? surname = null, 
            DayOffStatus[]? status = null, 
            DateTime? dateStart = null, 
            DateTime? dateEnd = null, 
            bool? isDeleted = false, 
            int? pageNumber = null,
            int? pageSize = null);
        Task<DayOffRequestDto> GetDayOffRequestByIdAsync(int requestId);
        Task<DayOffRequestDto> EditDayOffRequestAsync(int requestId, DateTime newStartDate, DateTime newEndDate, string? newReason, ClaimsPrincipal user);
        Task DeleteDayOffRequestAsync(int requestId);
        Task<DayOffRequestDto> RestoreDayOffRequestAsync(int requestId);
    }
}

