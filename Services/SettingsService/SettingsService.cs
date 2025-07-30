using Microsoft.EntityFrameworkCore;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Models;

public class SettingsService : ISettingsService
{
    private readonly WorkTimeDbContext _context;

    public SettingsService(WorkTimeDbContext context)
    {
        _context = context;
    }

    public async Task<Settings?> GetSettingsAsync()
    {
        return await _context.Settings.FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateSettingsAsync(Settings updated)
    {
        var settings = await _context.Settings.FirstOrDefaultAsync();
        if (settings == null)
            return false;

        settings.MaxBreakTime = updated.MaxBreakTime;
        settings.MaxWorkHoursDuringOneDay = updated.MaxWorkHoursDuringOneDay;
        settings.LatestStartMoment = updated.LatestStartMoment;
        settings.SyncUsersHour = updated.SyncUsersHour;
        settings.SyncUsersFrequency = updated.SyncUsersFrequency;
        settings.SyncUsersDays = updated.SyncUsersDays;

        await _context.SaveChangesAsync();
        return true;
    }
}