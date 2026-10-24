using RutaSV.Domain.Identity.Enum;

namespace RutaSV.Domain.Identity.Model;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public string PreferredLanguage { get; set; } = "es";
    public UserRole Role { get; set; } = UserRole.VISITANTE;
    public AuthProvider AuthProvider { get; set; } = AuthProvider.LOCAL;
    public string? ProviderId { get; set; }
    public UserStatus Status { get; set; } = UserStatus.PENDIENTE_VERIFICACION;
    public bool EmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<UserToken> Tokens { get; set; } = [];
}
