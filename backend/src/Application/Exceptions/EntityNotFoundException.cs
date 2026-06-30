using System.Globalization;

namespace Cinedex.Application.Exceptions;

public sealed class EntityNotFoundException(string entityName, object id)
    : Exception(string.Format(CultureInfo.InvariantCulture, "{0} with id '{1}' was not found.", entityName, id));