namespace Movies.WebService.IntegrationTests.Constants;

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
}
