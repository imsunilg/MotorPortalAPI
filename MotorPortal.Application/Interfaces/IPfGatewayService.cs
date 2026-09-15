namespace MotorPortal.Application.Interfaces;

/// <summary>
/// Abstraction over the external PF (payment facilitator) gateway. MockPfService simulates the
/// call today; swapping in a real HTTP-backed implementation later is a one-class change.
/// </summary>
public interface IPfGatewayService
{
    Task<PfConfirmation> ConfirmPaymentIntentAsync(long proposalId, decimal amount, CancellationToken cancellationToken = default);
}

public record PfConfirmation(bool Accepted, string ReferenceToken, string Message);
