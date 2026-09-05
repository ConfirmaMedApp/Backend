using AutoMapper;
using Backend.DTOs.DocumentTypes.Responses;
using Backend.Repositories.DocumentTypes;

namespace Backend.Services.DocumentTypes;

public class DocumentTypeService(IDocumentTypeRepository documentTypeRepository, IMapper mapper) : IDocumentTypeService
{
    public async Task<IEnumerable<DocumentTypeResponseDto>> GetAllAsync()
    {
        var documentTypes = await documentTypeRepository.GetAllAsync();
        return mapper.Map<IEnumerable<DocumentTypeResponseDto>>(documentTypes);
    }
}