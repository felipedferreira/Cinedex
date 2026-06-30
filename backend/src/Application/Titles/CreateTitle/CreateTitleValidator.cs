using FluentValidation;

namespace Movies.Application.Titles.CreateTitle;

internal sealed class CreateTitleValidator : AbstractValidator<CreateTitleCommand>
{
    public CreateTitleValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(command => command.Type)
            .IsInEnum();

        RuleFor(command => command.YearOfRelease)
            .InclusiveBetween(1888, DateTime.UtcNow.Year + 5);

        RuleFor(command => command.Description)
            .MaximumLength(2000);

        RuleForEach(command => command.GenreIds)
            .NotEmpty();
    }
}