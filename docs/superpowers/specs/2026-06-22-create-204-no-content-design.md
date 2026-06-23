# Design: Return 204 No Content for Create Operations

**Date:** 2026-06-22  
**Scope:** Movie and Genre creation endpoints  
**Status:** Design approved

## Overview

Modify `CreateMovieEndpoint` and `CreateGenreEndpoint` to return **204 No Content** with a Location header instead of **201 Created** with the full response body.

## Current Behavior

Both endpoints currently:
- Return HTTP 201 Created
- Include the full response object (MovieDetailsResponse or GenreResponse) in the response body
- Use `Send.CreatedAtAsync<TEndpoint>()` to generate the Location header automatically

**Current endpoint responses:**
- `POST /movies` → 201 Created + MovieDetailsResponse body
- `POST /genres` → 201 Created + GenreResponse body

## Desired Behavior

Both endpoints will:
- Return HTTP 204 No Content
- Include only the Location header pointing to the GetById endpoint
- Include no response body

**New endpoint responses:**
- `POST /movies` → 204 No Content + Location header (`/movies/{id}`)
- `POST /genres` → 204 No Content + Location header (`/genres/{id}`)

## Implementation Details

### Changes to CreateMovieEndpoint

1. Change class declaration from `Endpoint<CreateMoviesRequest, MovieDetailsResponse>` to `Endpoint<CreateMoviesRequest>`
2. Replace `Send.CreatedAtAsync<GetMovieByIdEndpoint>()` with manual Location header + `SendNoContentAsync()`

### Changes to CreateGenreEndpoint

1. Change class declaration from `Endpoint<CreateGenreRequest, GenreResponse>` to `Endpoint<CreateGenreRequest>`
2. Replace `Send.CreatedAtAsync<GetGenreByIdEndpoint>()` with manual Location header + `SendNoContentAsync()`

### Location Header Format

The Location header will be constructed from the resource ID:
- Movie: `/movies/{id}`
- Genre: `/genres/{id}`

This provides clients with the URI to retrieve the newly created resource via the GetById endpoints.

## Why 204 No Content?

- **Semantically correct**: 201 Created implies content is being returned; 204 No Content is proper for operations that don't return a body
- **Bandwidth efficient**: Clients don't receive data they already submitted
- **Single source of truth**: Data retrieval goes through the GetById endpoint, ensuring consistency
- **RESTful best practice**: Aligns with REST conventions for create-and-redirect patterns

## Testing Scope

- Verify CreateMovie returns 204 with Location header
- Verify CreateGenre returns 204 with Location header
- Verify Location header points to correct GetById endpoint
- Verify no response body is returned
- Existing integration tests will need Location header assertions updated

## Impact Analysis

- **No breaking changes** (verified: no clients depend on response body)
- **Clients**: Must stop parsing response body; start using Location header for GetById
- **API documentation**: Should be updated to reflect 204 response code
- **Tests**: Integration tests (CreateMovieEndpointTests, GenreEndpointTests) will need assertion updates

## Out of Scope

- Update operations (UpdateMovie, UpdateGenre) remain unchanged
- API documentation updates (separate task if needed)
- Migration guide for clients (verified as not needed)
