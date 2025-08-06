namespace TimeRecorderBACKEND.Dtos
{
    public class GroupWorkLogDtoWithUserNameAndSurname
    {
        public List<WorkLogDtoWithUserNameAndSurname> WorkLogs { get; set; } = new List<WorkLogDtoWithUserNameAndSurname>();
    }
}
