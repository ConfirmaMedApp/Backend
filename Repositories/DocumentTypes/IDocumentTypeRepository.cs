using Backend.Entities.DocumentTypes;

namespace Backend.Repositories.DocumentTypes;

public interface IDocumentTypeRepository
{
    Task<IEnumerable<DocumentType>> GetAllAsync();
}