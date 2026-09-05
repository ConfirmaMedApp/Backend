using Backend.Entities.Specialities;

namespace Backend.Repositories.Specialities;

public interface ISpecialityRepository
{
    Task<IEnumerable<Speciality>> GetAllAsync(int? limit, int? offset, bool? status, string search = "");
    Task<Speciality?> GetByIdAsync(int id);
    Task<bool> IsAlreadyUsedCode(string code, int? excludedId = null);
    Task<Speciality?> CreateAsync(Speciality speciality);
    Task<Speciality?> UpdateAsync(Speciality speciality);
}
