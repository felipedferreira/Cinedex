namespace Cinedex.WebService.Constants;

internal static class ApiConstants
{
    public const string BasePath = "/movies-svc";

    public static class RouteParameters
    {
        public const string Id = "id";
    }

    public static class Title
    {
        public const string Route = "titles";
        public const string RouteById = $"{Route}/{{{RouteParameters.Id}:guid}}";
        public const string Tag = "Titles";
        public const string GetByIdEndpointName = "GetTitleById";
    }

    public static class Genre
    {
        public const string Route = "genres";
        public const string RouteById = $"{Route}/{{{RouteParameters.Id}:guid}}";
        public const string Tag = "Genres";
        public const string GetByIdEndpointName = "GetGenreById";
    }

    public static class Auth
    {
        public const string Route = "auth";
        public const string Tag = "Auth";
        public const string RegisterRoute = $"{Route}/register";
        public const string LoginRoute = $"{Route}/login";
        public const string LogoutRoute = $"{Route}/logout";
        public const string ForgotPasswordRoute = $"{Route}/password/forgot";
        public const string ResetPasswordRoute = $"{Route}/password/reset";
    }

    public static class Health
    {
        public const string LiveRoute = "/health/live";
        public const string ReadyRoute = "/health/ready";
    }
}