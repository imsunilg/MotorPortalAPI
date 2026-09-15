using Microsoft.EntityFrameworkCore;
using MotorPortal.Application.DTOs;
using MotorPortal.Application.Exceptions;
using MotorPortal.Application.Interfaces;
using MotorPortal.Infrastructure.Data;

namespace MotorPortal.Infrastructure.Services;

public class MasterPolicyService : IMasterPolicyService
{
    private readonly AppDbContext _context;

    public MasterPolicyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MasterPolicyDto>> GetMasterPoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MasterPolicies.AsNoTracking()
            .OrderBy(m => m.MasterPolicyNo)
            .Select(m => new MasterPolicyDto
            {
                MasterPolicyId = m.MasterPolicyId,
                MasterPolicyNo = m.MasterPolicyNo,
                CustomerNo = m.CustomerNo,
                CdbgNo = m.CdbgNo,
                ProductId = m.ProductId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CdBalanceDto> GetCdBalanceAsync(long masterPolicyId, CancellationToken cancellationToken = default)
    {
        var masterPolicy = await _context.MasterPolicies.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MasterPolicyId == masterPolicyId, cancellationToken)
            ?? throw new NotFoundException($"Master policy {masterPolicyId} not found.");

        return new CdBalanceDto
        {
            MasterPolicyId = masterPolicy.MasterPolicyId,
            MasterPolicyNo = masterPolicy.MasterPolicyNo,
            CdBalance = masterPolicy.CdBalance
        };
    }
}
