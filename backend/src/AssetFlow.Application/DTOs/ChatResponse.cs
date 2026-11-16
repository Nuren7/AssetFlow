namespace AssetFlow.Application.DTOs;

public sealed record ChatResponse(string Answer, IReadOnlyList<ToolExecutionLog> ToolsUsed);
