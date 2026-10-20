using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Identity.Security;

public interface IJwtProvider
{
    string GenerateToken(User user);
}
