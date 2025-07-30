using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Dtos
{
    public class DayOffRequestDtoWithUserNameAndSurname
    {
        public int Id { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public DayOffStatus Status { get; set; }
        public string? Reason { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserSurname { get; set; }
    }
}
