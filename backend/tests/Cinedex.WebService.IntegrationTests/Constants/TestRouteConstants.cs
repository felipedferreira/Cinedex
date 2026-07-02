namespace Cinedex.WebService.IntegrationTests.Constants;

internal static class TestRouteConstants
{
    public const string MoviesServiceBasePath = "/movies-svc";

    public static class Title
    {
        public const string Endpoint = $"{MoviesServiceBasePath}/titles";
        public const string LocationPrefix = "/titles/";
    }

    public static class Genre
    {
        public const string Endpoint = $"{MoviesServiceBasePath}/genres";
        public const string LocationPrefix = "/genres/";
    }

    public static class Auth
    {
        public const string RegisterEndpoint = $"{MoviesServiceBasePath}/auth/register";
        public const string LoginEndpoint = $"{MoviesServiceBasePath}/auth/login";
        public const string LogoutEndpoint = $"{MoviesServiceBasePath}/auth/logout";
        public const string ForgotPasswordEndpoint = $"{MoviesServiceBasePath}/auth/password/forgot";
        public const string ResetPasswordEndpoint = $"{MoviesServiceBasePath}/auth/password/reset";
    }
}