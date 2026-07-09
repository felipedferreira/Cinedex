using Cinedex.Application.Validation;
using FluentValidation;

namespace Cinedex.Application.Genres.CreateGenre;

internal sealed class CreateGenreValidator : AbstractValidator<CreateGenreCommand>
{
    public CreateGenreValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage(ValidationMessages.GenreNameMustNotBeEmpty)
            .MaximumLength(100).WithMessage(ValidationMessages.GenreNameMustNotExceedLength);
    }
}
