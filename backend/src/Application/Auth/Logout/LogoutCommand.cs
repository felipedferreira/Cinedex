namespace Cinedex.Application.Auth.Logout;

public sealed record LogoutCommand(Guid RequestingUserId, string RefreshToken);
