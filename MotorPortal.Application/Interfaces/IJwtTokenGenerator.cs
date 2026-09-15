using MotorPortal.Domain.Entities;

namespace MotorPortal.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(UserMaster user);
}
