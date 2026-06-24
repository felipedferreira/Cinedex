using System.Globalization;
using Movies.Application.Constants;

namespace Movies.Application.Exceptions;

public sealed class EntityNotFoundException(string entityName, object id)
    : Exception(string.Format(CultureInfo.InvariantCulture, ExceptionConstants.EntityNotFoundMessageTemplate, entityName, id));
