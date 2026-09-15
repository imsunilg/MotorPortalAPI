using MotorPortal.Domain.Entities;

namespace MotorPortal.Application.Interfaces;

public interface IUserRepository : IRepository<UserMaster>
{
    Task<UserMaster?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
}
