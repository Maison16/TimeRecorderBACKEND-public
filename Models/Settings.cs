using System.Collections.Generic;
using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Models
{
    public class Settings
    {
        public int Id { get; set; } 
        public int MaxBreakTime { get; set; } = 30;
        public int MaxWorkHoursDuringOneDay { get; set; } = 10;
        public int LatestStartMoment { get; set; } = 12;

        public int SyncUsersHour { get; set; } = 2;
        public SyncFrequency SyncUsersFrequency { get; set; } = SyncFrequency.Daily;
        public List<SyncDayOfWeek> SyncUsersDays { get; set; } = new List<SyncDayOfWeek>(); // only for Weekly 
    }
}
