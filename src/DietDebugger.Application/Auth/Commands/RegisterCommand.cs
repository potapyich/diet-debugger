namespace DietDebugger.Application.Auth.Commands;

public record RegisterCommand(string Email, string Password);
public record LoginCommand(string Email, string Password);
public record RefreshCommand(string RefreshToken);

public record AuthResult(string AccessToken, string RefreshToken);
