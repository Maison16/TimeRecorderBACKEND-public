using Microsoft.EntityFrameworkCore;
using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public string Email { get; set; } = "";
        public ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
        public ICollection<DayOffRequest> DayOffRequests { get; set; } = new List<DayOffRequest>();
        public Project? Project { get; set; }
        public int? ProjectId { get; set; }
        public ExistenceStatus ExistenceStatus { get; set; } = ExistenceStatus.Exist;
    }

}
