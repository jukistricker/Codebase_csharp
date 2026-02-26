using Codebase.Models.Dtos.Requests.Auth;

namespace Codebase.Repositories.Interfaces;

public interface ISessionRepository
{
    Task StoreAsync(string token, UserSession session, TimeSpan ttl);
    Task DeleteAsync(string token);
}