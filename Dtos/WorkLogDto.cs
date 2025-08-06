using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Dtos
{
    public class WorkLogDto
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public WorkLogStatus Status { get; set; }
        public DateTime? EndTime { get; set; }
        public WorkLogType Type { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? Duration{ get; set; }
        public string? UserName { get; set; }
        public string? UserSurname { get; set; }

    }
}
