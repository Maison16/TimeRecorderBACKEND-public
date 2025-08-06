using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Models
{
    public class WorkLog
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public WorkLogStatus Status { get; set; } = WorkLogStatus.Started;
        public DateTime StartTime { get; set; }
        public WorkLogType Type { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ExistenceStatus ExistenceStatus { get; set; } = ExistenceStatus.Exist;
        public int? Duration{ get; }
    }

}
