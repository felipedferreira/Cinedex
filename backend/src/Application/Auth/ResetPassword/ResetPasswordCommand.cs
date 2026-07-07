namespace Cinedex.Application.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string ResetToken, string NewPassword);
