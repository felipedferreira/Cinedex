namespace Movies.Application.Exceptions;

public sealed class EntityNotFoundException(string entityName, object id)
    : Exception($"{entityName} with id '{id}' was not found.");
