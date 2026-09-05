using Backend.Entities.Genders;

namespace Backend.Repositories.Genders;

public interface IGenderRepository
{
    Task<IEnumerable<Gender>> GetAllAsync();
}