using AssetFlow.Application.DTOs;

namespace AssetFlow.Application.Services;

public interface IAiAssistantService
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
