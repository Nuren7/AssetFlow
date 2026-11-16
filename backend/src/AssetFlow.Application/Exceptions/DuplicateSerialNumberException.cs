namespace AssetFlow.Application.Exceptions;

public sealed class DuplicateSerialNumberException(string serialNumber)
    : Exception($"An asset with serial number '{serialNumber}' already exists.")
{
}
