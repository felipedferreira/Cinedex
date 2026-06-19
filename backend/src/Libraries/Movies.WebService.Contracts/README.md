# Movies Web Service Contracts

This NuGet package contains the DTOs (Data Transfer Objects) and contract classes for the Movies Web Service API.

## Installation

```bash
dotnet add package Movies.WebService.Contracts
```

## Contents

### Requests
- **CreateMoviesRequest**: DTO for creating a new movie
- **UpdateMoviesRequest**: DTO for updating a movie
- **CreateGenreRequest**: DTO for creating a new genre
- **UpdateGenreRequest**: DTO for updating a genre

### Responses
- **MovieResponse**: DTO for movie response data
- **MoviesResponse**: DTO for movies list response
- **GenreResponse**: DTO for genre response data
- **GenresResponse**: DTO for genres list response

## Usage Example

```csharp
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

var request = new CreateMoviesRequest
{
    Title = "Inception",
    YearOfRelease = 2010,
    Description = "A thief who steals corporate secrets through dream-sharing technology.",
    GenreIds = new[] { sciFiGenreId, thrillerGenreId }
};
```

Genres are managed as their own resource (`/genres`) and referenced from movies by id.

## License

MIT
