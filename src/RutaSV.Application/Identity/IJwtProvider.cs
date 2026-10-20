using RutaSV.Domain.Identity;

namespace RutaSV.Application.Identity;

public interface IJwtProvider
{
    string GenerateToken(User user);
}
