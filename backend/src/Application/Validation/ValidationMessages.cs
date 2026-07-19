namespace Cinedex.Application.Validation;

// Single home for validator error messages. Every AbstractValidator<T> in the application layer
// references these constants; adding or reusing a rule means adding or reusing a constant here.
//
// The messages are shipped verbatim to the client under the 400 response's per-field errors map,
// so they are a public contract: keep them terse, deliberately generic (no format hints that would
// leak credential shape from login), and stable across releases.
//
// FluentValidation placeholders such as {MaxLength}, {From}, {To} are substituted at validation
// time, so the actual limits are not duplicated between the rule and the message.
internal static class ValidationMessages
{
    // ---- Shared ----
    public const string IdMustNotBeEmpty = "Id must not be empty.";

    // ---- Auth ----
    public const string EmailMustNotBeEmpty = "Email must not be empty.";
    public const string EmailMustBeValid = "Email must be a valid email address.";
    public const string EmailMustNotExceedLength = "Email must be at most {MaxLength} characters.";

    public const string UsernameMustNotBeEmpty = "Username must not be empty.";
    public const string UsernameMustNotExceedLength = "Username must be at most {MaxLength} characters.";

    public const string PasswordMustNotBeEmpty = "Password must not be empty.";
    public const string PasswordMustNotExceedLength = "Password must be at most {MaxLength} characters.";

    public const string NewPasswordMustNotBeEmpty = "New password must not be empty.";
    public const string NewPasswordMustNotExceedLength = "New password must be at most {MaxLength} characters.";

    public const string ResetTokenMustNotBeEmpty = "Reset token must not be empty.";
    public const string RefreshTokenMustNotBeEmpty = "Refresh token must not be empty.";

    // ---- Titles ----
    public const string TitleMustNotBeEmpty = "Title must not be empty.";
    public const string TitleMustNotExceedLength = "Title must be at most {MaxLength} characters.";
    public const string TitleTypeMustBeRecognised = "Title type must be a recognised value.";
    public const string YearOfReleaseMustBeInRange = "Year of release must be between {From} and {To}.";
    public const string DescriptionMustNotExceedLength = "Description must be at most {MaxLength} characters.";
    public const string GenreIdMustNotBeEmpty = "Genre id must not be empty.";

    // ---- Genres ----
    public const string GenreNameMustNotBeEmpty = "Name must not be empty.";
    public const string GenreNameMustNotExceedLength = "Name must be at most {MaxLength} characters.";
}