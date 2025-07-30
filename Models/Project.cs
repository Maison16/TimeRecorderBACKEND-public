using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public ICollection<User> Users { get; set; }
        public ExistenceStatus ExistenceStatus { get; set; } = ExistenceStatus.Exist;
    }
}
