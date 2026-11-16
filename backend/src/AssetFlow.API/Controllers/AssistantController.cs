using AssetFlow.Application.DTOs;
using AssetFlow.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.API.Controllers;

[ApiController]
[Route("api/assistant")]
public sealed class AssistantController(IAiAssistantService assistantService) : ControllerBase
{
    [HttpPost("chat")]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Chat(
        ChatRequest request,
        CancellationToken cancellationToken) =>
        Ok(await assistantService.ChatAsync(request, cancellationToken));
}
