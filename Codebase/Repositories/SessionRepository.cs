using Codebase.Models.Dtos.Requests.Auth;
using Codebase.Repositories.Interfaces;
using Codebase.Utils;

namespace Codebase.Repositories;

using StackExchange.Redis;

public class SessionRepository : ISessionRepository
{
    private readonly IDatabase _redisDb;
    private const string KeyPrefix = "session:";

    public SessionRepository(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    public Task StoreAsync(string token, UserSession session, TimeSpan ttl)
    {
        return RedisUtil.SetObjectAsJsonAsync(
            _redisDb,
            $"{KeyPrefix}{token}",
            session,
            ttl
        );
    }

    public Task DeleteAsync(string token)
    {
        return _redisDb.KeyDeleteAsync($"{KeyPrefix}{token}");
    }
}