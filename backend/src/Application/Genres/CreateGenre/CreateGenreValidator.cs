using FluentValidation;

namespace Cinedex.Application.Genres.CreateGenre;

internal sealed class CreateGenreValidator : AbstractValidator<CreateGenreCommand>
{
    public CreateGenreValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}