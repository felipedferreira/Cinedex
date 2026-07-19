# Cinedex Web Service Contracts

This NuGet package contains the DTOs (Data Transfer Objects) and contract classes for the Cinedex Web Service API.

## Installation

```bash
dotnet add package Cinedex.WebService.Contracts
```

## Contents

### Requests
- **CreateTitlesRequest**: DTO for creating a new title
- **UpdateTitlesRequest**: DTO for updating a title
- **CreateGenreRequest**: DTO for creating a new genre
- **UpdateGenreRequest**: DTO for updating a genre
- **RegisterRequest**: DTO for registering a new user account
- **LoginRequest**: DTO for logging in with email and password
- **ForgotPasswordRequest**: DTO for requesting a password-reset email
- **ResetPasswordRequest**: DTO for resetting a password with a reset token

### Responses
- **TitleResponse**: DTO for title response data
- **TitleDetailsResponse**: DTO for a single title including its linked genres
- **TitlesResponse**: DTO for titles list response
- **GenreResponse**: DTO for genre response data
- **GenresResponse**: DTO for genres list response
- **LoginResponse**: DTO carrying the JWT access token (the refresh token travels only as an HttpOnly cookie)

### Enums
- **TitleType**: The kind of catalog title (`Movie`, `TvSeries`, `TvEpisode`, `Short`, `TvSpecial`, `Video`)

## Endpoint Response Behavior

Write operations do not return resource DTOs in the response body. Clients should use the
`Location` header to fetch the current representation when they need it.

| Operation | Status | Body | Location |
|-----------|--------|------|----------|
| `POST /titles` | `201 Created` | Empty | `/titles/{id}` |
| `POST /genres` | `201 Created` | Empty | `/genres/{id}` |
| `PUT /titles/{id}` | `202 Accepted` | Empty | `/titles/{id}` |
| `PUT /genres/{id}` | `202 Accepted` | Empty | `/genres/{id}` |
| `DELETE /titles/{id}` | `204 No Content` | Empty | None |
| `DELETE /genres/{id}` | `204 No Content` | Empty | None |

## Usage Example

```csharp
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;

var request = new CreateTitlesRequest
{
    Title = "Inception",
    YearOfRelease = 2010,
    Description = "A thief who steals corporate secrets through dream-sharing technology.",
    GenreIds = new[] { sciFiGenreId, thrillerGenreId }
};
```

Genres are managed as their own resource (`/genres`) and referenced from titles by id.

## License

MIT
