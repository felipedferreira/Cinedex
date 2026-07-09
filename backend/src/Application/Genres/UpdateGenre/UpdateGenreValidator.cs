using Cinedex.Application.Validation;
using FluentValidation;

namespace Cinedex.Application.Genres.UpdateGenre;

internal sealed class UpdateGenreValidator : AbstractValidator<UpdateGenreCommand>
{
    public UpdateGenreValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage(ValidationMessages.IdMustNotBeEmpty);

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage(ValidationMessages.GenreNameMustNotBeEmpty)
            .MaximumLength(100).WithMessage(ValidationMessages.GenreNameMustNotExceedLength);
    }
}
