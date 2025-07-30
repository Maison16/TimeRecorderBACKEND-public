using System.Threading.Tasks;

namespace TimeRecorderBACKEND.Services
{
    public interface ITeamsService
    {
        Task SendPrivateMessageAsync(string userAadId, string message);
    }
}