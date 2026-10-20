namespace TableFlow.Api.Data;

public sealed class ResourceConflictException(string message) : Exception(message);
