namespace Movies.WebService.Constants;

internal static class ProblemDetailsConstants
{
    public const string BadRequestType = "https://httpwg.org/specs/rfc7231.html#status.400";
    public const string BadRequestTitle = "Bad Request";
    public const string ValidationErrorDetail = "One or more validation errors occurred.";
    public const string NotFoundType = "https://httpwg.org/specs/rfc7231.html#status.404";
    public const string NotFoundTitle = "Not Found";
    public const string InternalServerErrorType = "https://httpwg.org/specs/rfc7231.html#status.500";
    public const string InternalServerErrorTitle = "Internal Server Error";
    public const string InternalServerErrorDetail = "An unhandled exception occurred.";
}
