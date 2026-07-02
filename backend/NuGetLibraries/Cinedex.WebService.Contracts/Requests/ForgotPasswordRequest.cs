namespace Cinedex.WebService.Contracts.Requests;

public class ForgotPasswordRequest
{
    public required string Email { get; init; } = string.Empty;
}
