namespace TimeRecorderBACKEND.Dto
{
    public class RequestDayOffModel
    {
        public Guid? UserId { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public string? Reason { get; set; }
    }
}