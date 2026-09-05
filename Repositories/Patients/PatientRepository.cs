using Backend.DTOs.Patients.Responses;
using Backend.Entities.Patients;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.Patients;

public class PatientRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IPatientRepository
{
    public Task<PatientFlatDto?> CreateAsync(Patient patient)
    {
        const string command = "SELECT * FROM create_patient(@Name, @Lastname, @Email, @Phone, @Birthdate::date, @Document, @DocumentTypeId, @GenderId);";

        return ExecuteSafeAsync(async conn =>
        {
            var createdPatient = await conn.ExecuteScalarAsync<int>(command, new
            {
                Name = patient.Name.ToLower(),
                Lastname = patient.Lastname.ToLower(),
                Email = patient.Email.ToLower(),
                Phone = patient.Phone.ToLower(),
                patient.Birthdate,
                Document = patient.Document.ToLower(),
                patient.DocumentTypeId,
                patient.GenderId
            });

            return await GetByIdAsync(createdPatient);
        }, dbConnectionFactory);
    }

    public Task<IEnumerable<PatientFlatDto>> GetAllAsync(int? limit, int? offset, string search = "")
    {
        const string query = "SELECT * FROM get_all_patients(@Limit, @Offset, @Search);";

        return ExecuteSafeAsync(async conn =>
        {
            var patients = await conn.QueryAsync<PatientFlatDto>(query, new
            {
                Limit = limit,
                Offset = offset,
                Search = search
            });
            return patients;
        }, dbConnectionFactory);
    }

    public Task<PatientFlatDto?> GetByDocumentAsync(string document)
    {
        const string query = "SELECT * FROM get_patient_by_document(@Document::varchar);";

        return ExecuteSafeAsync(async conn =>
        {
            var patient = await conn.QueryFirstOrDefaultAsync<PatientFlatDto>(query, new { Document = document });
            return patient ?? null;
        }, dbConnectionFactory);
    }

    public Task<PatientFlatDto?> GetByIdAsync(int id)
    {
        const string query = "SELECT * FROM get_patient_by_id(@Id);";

        return ExecuteSafeAsync(async conn =>
        {
            var patient = await conn.QueryFirstOrDefaultAsync<PatientFlatDto>(query, new { Id = id });

            return patient ?? null;
        }, dbConnectionFactory);
    }

    public Task<PatientFlatDto?> UpdateAsync(Patient patient)
    {
        const string command = "SELECT * FROM update_patient(@Id, @Name, @Lastname, @Email, @Phone, @Birthdate::date, @Document, @DocumentTypeId, @GenderId);";
        return ExecuteSafeAsync(async conn =>
        {
            var updatedPatient = await conn.ExecuteScalarAsync<int>(command, new
            {
                patient.Id,
                Name = patient.Name.ToLower(),
                Lastname = patient.Lastname.ToLower(),
                Email = patient.Email.ToLower(),
                Phone = patient.Phone.ToLower(),
                patient.Birthdate,
                Document = patient.Document.ToLower(),
                patient.DocumentTypeId,
                patient.GenderId
            });
            return await GetByIdAsync(updatedPatient);
        }, dbConnectionFactory);
    }

    public Task<bool> DocumentAndDocumentTypeAlreadyExists(string document, int documentTypeId, int? excludingPatientId = null)
    {
        const string query = "SELECT * FROM document_document_type_already_used_patients(@Document, @DocumentTypeId, @ExcludingPatientId);";
        return ExecuteSafeAsync(async conn =>
        {
            var exists = await conn.ExecuteScalarAsync<bool>(query, new { Document = document, DocumentTypeId = documentTypeId, ExcludingPatientId = excludingPatientId });
            return exists;
        }, dbConnectionFactory);
    }
}
