namespace TimeRecorderBACKEND.Dtos
{
    public class SummaryDto
    {
        // Work summary
        public int TotalWorkTimeMinutes { get; set; }
        public int TotalBreakTimeMinutes { get; set; }
        public int WorkLogCount { get; set; }
        public int BreakCount { get; set; }

        // Day off summary
        public int DayOffRequestCount { get; set; }
        public int ExecutedDaysOff { get; set; }
        public int ApprovedDaysOff { get; set; }
        public int RejectedDaysOff { get; set; }
        public int PendingDaysOff { get; set; }
        public int CancelledDaysOff { get; set; }
        public string? UserName { get; set; }
        public string? UserSurname { get; set; }
        public string? UserEmail { get; set; }
        public DateTime Date { get; set; }
    }
}
