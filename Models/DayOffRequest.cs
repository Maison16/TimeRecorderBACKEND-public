using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Models
{
    public class DayOffRequest
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public DayOffStatus Status { get; set; } = DayOffStatus.Pending;
        public string? Reason { get; set; }
        public ExistenceStatus ExistenceStatus { get; set; } = ExistenceStatus.Exist;
    }
}
