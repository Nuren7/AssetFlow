namespace AssetFlow.Application.Exceptions;

public sealed class AssetValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more asset fields are invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
