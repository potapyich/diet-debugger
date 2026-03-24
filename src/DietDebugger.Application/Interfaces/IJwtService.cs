using DietDebugger.Domain.Entities;

namespace DietDebugger.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
