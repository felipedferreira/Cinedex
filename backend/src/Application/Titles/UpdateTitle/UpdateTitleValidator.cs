using FluentValidation;

namespace Cinedex.Application.Titles.UpdateTitle;

internal sealed class UpdateTitleValidator : AbstractValidator<UpdateTitleCommand>
{
    public UpdateTitleValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

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