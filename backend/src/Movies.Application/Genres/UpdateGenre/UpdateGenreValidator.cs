using FluentValidation;

namespace Movies.Application.Genres.UpdateGenre;

internal sealed class UpdateGenreValidator : AbstractValidator<UpdateGenreCommand>
{
    public UpdateGenreValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Description)
            .MaximumLength(2000);
    }
}
