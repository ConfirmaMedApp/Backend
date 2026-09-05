using Backend.DTOs.DocumentTypes.Responses;

namespace Backend.Services.DocumentTypes;

public interface IDocumentTypeService
{
    Task<IEnumerable<DocumentTypeResponseDto>> GetAllAsync();
}