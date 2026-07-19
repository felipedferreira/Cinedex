using Cinedex.Application.Validation;
using FluentValidation;

namespace Cinedex.Application.Titles.CreateTitle;

internal sealed class CreateTitleValidator : AbstractValidator<CreateTitleCommand>
{
    public CreateTitleValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage(ValidationMessages.TitleMustNotBeEmpty)
            .MaximumLength(256).WithMessage(ValidationMessages.TitleMustNotExceedLength);

        RuleFor(command => command.Type)
            .IsInEnum().WithMessage(ValidationMessages.TitleTypeMustBeRecognised);

        RuleFor(command => command.YearOfRelease)
            .InclusiveBetween(1888, DateTime.UtcNow.Year + 5)
                .WithMessage(ValidationMessages.YearOfReleaseMustBeInRange);

        RuleFor(command => command.Description)
            .MaximumLength(2000).WithMessage(ValidationMessages.DescriptionMustNotExceedLength);

        RuleForEach(command => command.GenreIds)
            .NotEmpty().WithMessage(ValidationMessages.GenreIdMustNotBeEmpty);
    }
}