using TimeRecorderBACKEND.Models;

public interface ISettingsService
{
    Task<Settings?> GetSettingsAsync();
    Task<bool> UpdateSettingsAsync(Settings updated);
}