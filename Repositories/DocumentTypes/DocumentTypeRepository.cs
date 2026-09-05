using Backend.Entities.DocumentTypes;
using Backend.Persistence;
using Dapper;

namespace Backend.Repositories.DocumentTypes;

public class DocumentTypeRepository(IDbConnectionFactory dbConnectionFactory) : RepositoryGuard, IDocumentTypeRepository
{
    public Task<IEnumerable<DocumentType>> GetAllAsync()
    {
        const string query = "SELECT * FROM get_all_document_types();";
        return ExecuteSafeAsync(async conn => await conn.QueryAsync<DocumentType>(query),
            dbConnectionFactory);
    }
}