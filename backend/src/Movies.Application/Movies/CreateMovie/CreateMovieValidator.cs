using FluentValidation;

namespace Movies.Application.Movies.CreateMovie;

internal sealed class CreateMovieValidator : AbstractValidator<CreateMovieCommand>
{
    public CreateMovieValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(command => command.YearOfRelease)
            .InclusiveBetween(1888, DateTime.UtcNow.Year + 5);

        RuleFor(command => command.Description)
            .MaximumLength(2000);

        RuleForEach(command => command.GenreIds)
            .NotEmpty();
    }
}
