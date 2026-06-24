namespace Movies.Application.Constants;

internal static class LogMessageConstants
{
    public static class Title
    {
        public const string Creating = "Creating title {Title} ({YearOfRelease}).";
        public const string Created = "Created title {TitleId} ({Title}).";
        public const string Updating = "Updating title {TitleId}.";
        public const string NotFoundForUpdate = "Title {TitleId} was not found for update.";
        public const string Updated = "Updated title {TitleId}.";
        public const string Deleting = "Deleting title {TitleId}.";
        public const string NotFoundForDeletion = "Title {TitleId} was not found for deletion.";
        public const string Deleted = "Deleted title {TitleId}.";
        public const string NotFound = "Title {TitleId} was not found.";
        public const string Retrieved = "Retrieved title {TitleId}.";
        public const string RetrievedAll = "Retrieved {TitleCount} titles.";
    }

    public static class Genre
    {
        public const string Creating = "Creating genre {Name}.";
        public const string Created = "Created genre {GenreId} ({Name}).";
        public const string Updating = "Updating genre {GenreId}.";
        public const string NotFoundForUpdate = "Genre {GenreId} was not found for update.";
        public const string Updated = "Updated genre {GenreId}.";
        public const string Deleting = "Deleting genre {GenreId}.";
        public const string NotFoundForDeletion = "Genre {GenreId} was not found for deletion.";
        public const string Deleted = "Deleted genre {GenreId}.";
        public const string NotFound = "Genre {GenreId} was not found.";
        public const string Retrieved = "Retrieved genre {GenreId}.";
        public const string RetrievedAll = "Retrieved {GenreCount} genres.";
    }
}
