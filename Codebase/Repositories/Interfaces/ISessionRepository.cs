using Codebase.Models.Dtos.Responses;

namespace Codebase.Repositories.Interfaces;

public interface ISessionRepository
{
    Task StoreAsync(string jti, UserSession session, TimeSpan ttl);
    Task DeleteAsync(string jti);
    Task<UserSession?> GetAsync(string jti);
}