---
sidebar_position: 2
---

# Movie Catalog & API

## Genres and titles

Genres are their own entity (`Id`, `Name`, `Description`) stored in the `genres` table, and titles
link to genres through a many-to-many relationship backed by a `movie_genres` junction table. A
genre's navigation is one-directional — a title knows its genres, but a genre does not hold a
back-reference to titles.

- **CRUD endpoints** under `/movies-svc/genres` (`GET`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`)
  and the equivalent under `/movies-svc/titles`.
- **Titles reference genres by id** — `CreateTitlesRequest`/`UpdateTitlesRequest` carry a `GenreIds`
  collection, and title responses include the linked genres.
- **Seeded data** — the database ships with 17 common genres (Action, Comedy, Drama, …) so titles
  can be tagged immediately.
- **`TitleType`** distinguishes the kind of catalog entry: `Movie`, `TvSeries`, `TvEpisode`, `Short`,
  `TvSpecial`, `Video`.

The catalog is **members-only**: every Genre and Title endpoint, reads and writes alike, requires a
bearer token. See [Security → Overview](../security/overview.md) for the full endpoint table and
[Security → Known gaps](../security/known-gaps.md) for what that does and doesn't currently enforce
(any logged-in account can edit the catalog — there's no role restriction yet).

## Request/response contracts

The API contracts ship as their own NuGet package,
[`FoundryOceanus.WebService.Contracts`](https://github.com/felipedferreira/Cinedex/blob/main/backend/NuGetLibraries/Contracts/FoundryOceanus.WebService.Contracts/README.md).

**Requests:** `CreateTitlesRequest`, `UpdateTitlesRequest`, `CreateGenreRequest`, `UpdateGenreRequest`,
plus the auth requests covered in [Security](../security/overview.md).

**Responses:** `TitleResponse`, `TitleDetailsResponse` (a single title including its linked genres),
`TitlesResponse`, `GenreResponse`, `GenresResponse`.

Write operations don't return resource DTOs in the body — clients use the `Location` header to fetch
the current representation when they need it:

| Operation             | Status           | Body  | Location       |
| --------------------- | ---------------- | ----- | -------------- |
| `POST /titles`        | `201 Created`    | Empty | `/titles/{id}` |
| `POST /genres`        | `201 Created`    | Empty | `/genres/{id}` |
| `PUT /titles/{id}`    | `202 Accepted`   | Empty | `/titles/{id}` |
| `PUT /genres/{id}`    | `202 Accepted`   | Empty | `/genres/{id}` |
| `DELETE /titles/{id}` | `204 No Content` | Empty | None           |
| `DELETE /genres/{id}` | `204 No Content` | Empty | None           |

```csharp
using FoundryOceanus.WebService.Contracts.Requests;

var request = new CreateTitlesRequest
{
    Title = "Inception",
    YearOfRelease = 2010,
    Description = "A thief who steals corporate secrets through dream-sharing technology.",
    GenreIds = new[] { sciFiGenreId, thrillerGenreId }
};
```

## Trying it live

With the stack running (see [Overview](./overview.md#two-ways-to-run-it)):

- **API docs:** https://localhost:9000/movies-svc/api-docs/v1 (Scalar UI)
- **OpenAPI spec:** https://localhost:9000/movies-svc/openapi/v1.json

Every catalog call needs a bearer token first — register and log in via the
[auth endpoints](../security/overview.md#endpoints).
