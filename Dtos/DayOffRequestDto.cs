using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Dtos
{
    public class DayOffRequestDto
    {
        public int Id { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public DayOffStatus Status { get; set; }
        public string? Reason { get; set; }
        public Guid UserId { get; set; }
    }
}
