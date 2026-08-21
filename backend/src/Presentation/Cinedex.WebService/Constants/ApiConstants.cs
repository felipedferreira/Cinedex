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
        public const string RefreshRoute = $"{Route}/refresh";
        public const string LogoutRoute = $"{Route}/logout";

        // Nested under the auth route rather than a sibling /sessions, and that is load-bearing: the
        // refresh cookie's Path is /movies-svc/auth, so a route outside it would neither receive the
        // cookie nor be able to clear it.
        public const string RevokeAllSessionsRoute = $"{Route}/sessions/all";
        public const string ForgotPasswordRoute = $"{Route}/password/forgot";
        public const string ResetPasswordRoute = $"{Route}/password/reset";
    }

    public static class Security
    {
        // The key this scheme is filed under in components.securitySchemes. Scalar's preferred-scheme
        // setting refers to it by the same name, so the two must not drift apart.
        public const string BearerScheme = "Bearer";
    }

    public static class Health
    {
        public const string LiveRoute = "/health/live";
        public const string ReadyRoute = "/health/ready";
    }
}