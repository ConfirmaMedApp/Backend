using Backend.Entities.Durations;

namespace Backend.Repositories.Durations;

public interface IDurationRepository
{
    Task<IEnumerable<Duration>> GetAllAsync();
}
