namespace AssetFlow.Application.Exceptions;

public sealed class AssetNotFoundException(Guid id)
    : Exception($"Asset '{id}' was not found.")
{
}
