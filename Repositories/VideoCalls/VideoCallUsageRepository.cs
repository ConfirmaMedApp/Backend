using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.VideoCalls;

public class VideoCallUsageRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IVideoCallUsageRepository
{
    public Task<bool> TryReserveMonthlySlotAsync(int year, int month, int limit)
    {
        const string query = "SELECT try_reserve_video_call_slot(@Year::smallint, @Month::smallint, @Limit);";

        return ExecuteSafeAsync(async conn =>
                await conn.ExecuteScalarAsync<bool>(query, new
                {
                    Year = (short)year,
                    Month = (short)month,
                    Limit = limit
                }),
            dbConnectionFactory);
    }

    public Task ReleaseMonthlySlotAsync(int year, int month)
    {
        const string command = "SELECT release_video_call_slot(@Year::smallint, @Month::smallint);";

        return ExecuteSafeAsync(async conn =>
                await conn.ExecuteAsync(command, new
                {
                    Year = (short)year,
                    Month = (short)month
                }),
            dbConnectionFactory);
    }
}
