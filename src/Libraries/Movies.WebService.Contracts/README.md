# Movies Web Service Contracts

This NuGet package contains the DTOs (Data Transfer Objects) and contract classes for the Movies Web Service API.

## Installation

```bash
dotnet add package Movies.WebService.Contracts
```

## Contents

### Enums
- **Genre**: Movie genres (Action, Comedy, Drama, Fantasy, Horror, Romance, SciFi, Thriller, Animation, Adventure, Crime, Documentary, Family, History, Musical, Mystery, Western)

### Requests
- **CreateMoviesRequest**: DTO for creating a new movie
- **UpdateMoviesRequest**: DTO for updating a movie

### Responses
- **MovieResponse**: DTO for movie response data
- **MoviesResponse**: DTO for movies list response

## Usage Example

```csharp
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;
using Movies.WebService.Contracts.Enums;

var request = new CreateMoviesRequest
{
    Title = "Inception",
    YearOfRelease = 2010,
    Genres = new[] { Genre.SciFi, Genre.Thriller }
};
```

## License

MIT
