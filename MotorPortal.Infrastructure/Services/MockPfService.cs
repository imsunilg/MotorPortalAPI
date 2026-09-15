using Microsoft.Extensions.Logging;
using MotorPortal.Application.Interfaces;

namespace MotorPortal.Infrastructure.Services;

/// <summary>
/// Simulates the external PF (payment facilitator) gateway confirming a payment intent before
/// the DB-side sp_tag_payment call actually debits the master policy's CD balance. Architected so
/// swapping in a real HTTP client later only requires a new class implementing IPfGatewayService.
/// </summary>
public class MockPfService : IPfGatewayService
{
    private readonly ILogger<MockPfService> _logger;

    public MockPfService(ILogger<MockPfService> logger)
    {
        _logger = logger;
    }

    public async Task<PfConfirmation> ConfirmPaymentIntentAsync(long proposalId, decimal amount, CancellationToken cancellationToken = default)
    {
        // simulate network latency of a real external PF gateway call
        await Task.Delay(150, cancellationToken);

        var referenceToken = $"PFINTENT-{DateTime.UtcNow:yyyyMMddHHmmss}-{proposalId}";

        _logger.LogInformation(
            "MockPfService confirmed payment intent for proposal {ProposalId}, amount {Amount}, token {Token}",
            proposalId, amount, referenceToken);

        return new PfConfirmation(true, referenceToken, "Payment intent confirmed by PF gateway (simulated).");
    }
}
