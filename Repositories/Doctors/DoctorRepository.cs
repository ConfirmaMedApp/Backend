using Backend.DTOs.Doctors.Responses;
using Backend.Entities.Doctors;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Doctors;

public class DoctorRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IDoctorRepository
{
    public Task<IEnumerable<DoctorFlatDto>> GetAllAsync(int? limit, int? offset, bool? status, string search = "")
    {
        const string query = "SELECT * FROM get_all_doctors(@Limit, @Offset, @Search, @Status);";
        return ExecuteSafeAsync(async conn => await conn.QueryAsync<DoctorFlatDto>(query, new { Limit = limit, Offset = offset, Search = search, Status = status }), 
            dbConnectionFactory);
    }

    public Task<DoctorFlatDto?> GetByIdAsync(int id)
    {
        const string query = "SELECT * FROM get_doctor_by_id(@Id);";
        return ExecuteSafeAsync(async conn =>
        {
            var doctor = await conn.QueryFirstOrDefaultAsync<DoctorFlatDto>(query, new { Id = id });
            return doctor ?? null;
        }, dbConnectionFactory);
    }

    public Task<DoctorFlatDto?> CreateAsync(Doctor doctor)
    {
        const string command = "SELECT * FROM create_doctor(@Name, @LastName, @Document, @DocumentTypeId, @Email, @Status);";
        return ExecuteSafeAsync(async conn =>
        {
            var createdDoctor = await conn.ExecuteScalarAsync<int>(command, new
            {
                Name = doctor.Name.ToLower(),
                LastName = doctor.LastName.ToLower(),
                Document = doctor.Document.ToLower(),
                doctor.DocumentTypeId,
                Email = doctor.Email.ToLower(),
                doctor.Status
            });

            return await GetByIdAsync(createdDoctor);
            
        }, dbConnectionFactory);
    }

    public Task<DoctorFlatDto?> UpdateAsync(Doctor doctor)
    {
        const string command = "SELECT * FROM update_doctor(@Id, @Name, @LastName, @Document, @DocumentTypeId, @Email, @Status);";
        return ExecuteSafeAsync(async conn =>
        {
            var updatedDoctor = await conn.ExecuteScalarAsync<int>(command, new
            {
                doctor.Id,
                Name = doctor.Name.ToLower(),
                LastName = doctor.LastName.ToLower(),
                Document = doctor.Document.ToLower(),
                doctor.DocumentTypeId,
                Email = doctor.Email.ToLower(),
                doctor.Status
            });

            return await GetByIdAsync(updatedDoctor);
            
        }, dbConnectionFactory);
    }

    public Task<bool> IsDocumentAndDocumentTypeCombinationUnique(string document, int documentTypeId, int? excludedId = null)
    {
        const string query = "SELECT * FROM document_and_document_type_already_used(@Document, @DocumentTypeId, @ExcludedId);";
        return ExecuteSafeAsync(async conn =>
        {
            var exists = await conn.ExecuteScalarAsync<bool>(query, new
            {
                Document = document,
                DocumentTypeId = documentTypeId,
                ExcludedId = excludedId
            });
            return exists;
        }, dbConnectionFactory);
    }
}