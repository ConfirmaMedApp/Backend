using Backend.Entities.Offices;

namespace Backend.Repositories.Offices;

public interface IOfficeRepository
{
    Task<IEnumerable<Office>> GetAllAsync();
    Task<Office?> GetByIdAsync(int id);
    Task<Office?> GetByUserIdAsync(int userId);
    Task<Office?> CreateAsync(Office office);
    Task<Office?> UpdateAsync(Office office);

    // Methods for verfiying validations
    Task<bool> IsNitUniqueAsync(string nit, int? excludingOfficeId = null);
}