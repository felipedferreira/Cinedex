using FluentValidation;

namespace Movies.Application.Genres.CreateGenre;

internal sealed class CreateGenreValidator : AbstractValidator<CreateGenreCommand>
{
    public CreateGenreValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Description)
            .MaximumLength(2000);
    }
}
