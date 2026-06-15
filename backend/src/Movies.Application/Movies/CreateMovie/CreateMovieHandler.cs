using FluentValidation;
using Movies.Application.Abstractions;
using Movies.Domain;

namespace Movies.Application.Movies.CreateMovie;

internal sealed class CreateMovieHandler(IMovieRepository repository, IValidator<CreateMovieCommand> validator) : ICreateMovieHandler
{
    public async Task<MovieDto> Handle(CreateMovieCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var movie = new Movie
        {
            Title = command.Title,
            YearOfRelease = command.YearOfRelease,
            Description = command.Description,
        };

        var created = await repository.CreateAsync(movie, cancellationToken);

        return created.ToDto();
    }
}
