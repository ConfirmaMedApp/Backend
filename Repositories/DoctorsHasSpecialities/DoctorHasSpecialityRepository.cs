using Backend.DTOs.Doctors.Responses;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.DoctorsHasSpecialities;

public class DoctorHasSpecialityRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IDoctorHasSpecialityRepository
{
    public Task<IEnumerable<DoctorFlatDto>> CreateAsync(int specialityId, int[]? doctorIds)
    {
        const string command = "SELECT * FROM add_or_update_doctors_to_speciality(@SpecialityId, @DoctorIds);";

        return ExecuteSafeAsync(async conn =>
        {
            var doctorHasSpecialities = await conn.ExecuteScalarAsync<int>(command, new
            {
                SpecialityId = specialityId,
                DoctorIds = doctorIds
            });

            return await GetBySpecialityIdAsync(specialityId);
        }, dbConnectionFactory);
    }

    public Task<bool> ExistsManyDoctorsAsync(int[] doctorIds)
    {
        const string query = "SELECT * FROM doctors_exists(@DoctorIds);";
        return ExecuteSafeAsync(async conn =>
        {
            var exists = await conn.ExecuteScalarAsync<bool>(query, new
            {
                DoctorIds = doctorIds
            });

            return exists;
        }, dbConnectionFactory);
    }

    public Task<bool> ExistsSpecialityAsync(int specialityId)
    {
        const string query = "SELECT * FROM speciality_exists(@SpecialityId);";

        return ExecuteSafeAsync(async conn =>
        {
            var exists = await conn.ExecuteScalarAsync<bool>(query, new
            {
                SpecialityId = specialityId
            });

            return exists;
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<DoctorFlatDto>> GetBySpecialityIdAsync(int specialityId)
    {
        const string query = "SELECT * FROM get_doctors_by_speciality(@SpecialityId);";

        return ExecuteSafeAsync(async conn =>
        {
            var doctors = await conn.QueryAsync<DoctorFlatDto>(query, new
            {
                SpecialityId = specialityId
            });

            return doctors;
        }, dbConnectionFactory);
    }
}
