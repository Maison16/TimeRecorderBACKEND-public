using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Dtos
{
    public class WorkLogDtoWithUserNameAndSurname
    {
        public int Id { get; set; }
        public WorkLogStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public WorkLogType Type { get; set; }
        public Guid UserId { get; set; }
        public int Duration { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserName { get; set; }
        public string? UserSurname { get; set; }
    }
}