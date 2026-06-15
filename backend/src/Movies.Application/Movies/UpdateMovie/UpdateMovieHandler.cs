using FluentValidation;
using Movies.Application.Abstractions;
using Movies.Domain;

namespace Movies.Application.Movies.UpdateMovie;

internal sealed class UpdateMovieHandler(IMovieRepository repository, IValidator<UpdateMovieCommand> validator) : IUpdateMovieHandler
{
    public async Task<MovieDto?> Handle(UpdateMovieCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var movie = new Movie
        {
            Id = command.Id,
            Title = command.Title,
            YearOfRelease = command.YearOfRelease,
            Description = command.Description,
        };

        var updated = await repository.UpdateAsync(movie, cancellationToken);

        return updated ? movie.ToDto() : null;
    }
}
