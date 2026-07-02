namespace Cinedex.WebService.Contracts.Requests;

public class ResetPasswordRequest
{
    public required string Email { get; init; } = string.Empty;

    public required string ResetToken { get; init; } = string.Empty;

    public required string NewPassword { get; init; } = string.Empty;
}
