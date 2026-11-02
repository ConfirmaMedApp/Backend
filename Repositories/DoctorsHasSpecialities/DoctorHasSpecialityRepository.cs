using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.DoctorsHasSpecialities.Responses;
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

    public Task<bool> ExistsRelationAsync(int doctorId, int specialityId)
    {
        const string query = """
            SELECT EXISTS(
                SELECT 1 FROM doctors_has_specialities
                WHERE doctor_id = @DoctorId AND speciality_id = @SpecialityId
            );
            """;

        return ExecuteSafeAsync(async conn =>
            await conn.ExecuteScalarAsync<bool>(query, new { DoctorId = doctorId, SpecialityId = specialityId }),
            dbConnectionFactory);
    }

    public Task<DoctorSpecialityPriceResponseDto?> SetPriceAsync(int doctorId, int specialityId, decimal priceUsd)
    {
        const string command = """
            UPDATE doctors_has_specialities
            SET price_usd = @PriceUsd
            WHERE doctor_id = @DoctorId AND speciality_id = @SpecialityId
            RETURNING
                id            AS "Id",
                doctor_id     AS "DoctorId",
                speciality_id AS "SpecialityId",
                price_usd     AS "PriceUsd",
                status        AS "Status";
            """;

        return ExecuteSafeAsync(async conn =>
            await conn.QueryFirstOrDefaultAsync<DoctorSpecialityPriceResponseDto>(command,
                new { DoctorId = doctorId, SpecialityId = specialityId, PriceUsd = priceUsd }),
            dbConnectionFactory);
    }

    public Task<IEnumerable<DoctorSpecialityPriceResponseDto>> GetPricesAsync(int? doctorId, int? specialityId)
    {
        const string query = """
            SELECT
                id            AS "Id",
                doctor_id     AS "DoctorId",
                speciality_id AS "SpecialityId",
                price_usd     AS "PriceUsd",
                status        AS "Status"
            FROM doctors_has_specialities
            WHERE (@DoctorId IS NULL OR doctor_id = @DoctorId)
              AND (@SpecialityId IS NULL OR speciality_id = @SpecialityId)
            ORDER BY doctor_id, speciality_id;
            """;

        return ExecuteSafeAsync(async conn =>
            await conn.QueryAsync<DoctorSpecialityPriceResponseDto>(query,
                new { DoctorId = doctorId, SpecialityId = specialityId }),
            dbConnectionFactory);
    }
}
