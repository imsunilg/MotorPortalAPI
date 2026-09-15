using Microsoft.EntityFrameworkCore;
using MotorPortal.Application.Interfaces;
using MotorPortal.Domain.Entities;
using MotorPortal.Infrastructure.Data;

namespace MotorPortal.Infrastructure.Repositories;

public class UserRepository : Repository<UserMaster>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserMaster?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }
}
