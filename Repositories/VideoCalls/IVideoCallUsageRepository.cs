namespace Backend.Repositories.VideoCalls;

public interface IVideoCallUsageRepository
{
    Task<bool> TryReserveMonthlySlotAsync(int year, int month, int limit);
    Task ReleaseMonthlySlotAsync(int year, int month);
}
