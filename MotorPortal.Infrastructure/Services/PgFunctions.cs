using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MotorPortal.Infrastructure.Data;

namespace MotorPortal.Infrastructure.Services;

/// <summary>
/// Thin wrapper around the PostgreSQL functions/procedures owned by MotorPortalDB. We call them via
/// raw ADO commands on the AppDbContext's own connection rather than re-implementing their logic in C#.
/// </summary>
internal static class PgFunctions
{
    private const string Schema = "\"SGInsurance\"";

    public static async Task<decimal> CalculateNetPremiumAsync(AppDbContext context, decimal basePremium, decimal addonPremium, decimal discount, CancellationToken cancellationToken)
    {
        return await WithConnectionAsync(context, async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {Schema}.fn_calculate_net_premium(@b, @a, @d)";
            cmd.Parameters.Add(new NpgsqlParameter("b", basePremium));
            cmd.Parameters.Add(new NpgsqlParameter("a", addonPremium));
            cmd.Parameters.Add(new NpgsqlParameter("d", discount));
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToDecimal(result);
        }, cancellationToken);
    }

    public static async Task<(decimal GstAmount, decimal FinalPremium)> CalculateGstAsync(AppDbContext context, decimal netPremium, decimal gstRate, CancellationToken cancellationToken)
    {
        return await WithConnectionAsync(context, async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT gst_amount, final_premium FROM {Schema}.fn_calculate_gst(@n, @r)";
            cmd.Parameters.Add(new NpgsqlParameter("n", netPremium));
            cmd.Parameters.Add(new NpgsqlParameter("r", gstRate));
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return (reader.GetDecimal(0), reader.GetDecimal(1));
            }
            throw new InvalidOperationException("fn_calculate_gst returned no rows.");
        }, cancellationToken);
    }

    public static async Task<string> GenerateProposalNoAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        return await WithConnectionAsync(context, async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {Schema}.fn_generate_proposal_no()";
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return (string)result!;
        }, cancellationToken);
    }

    public static async Task<string> GeneratePolicyNoAsync(AppDbContext context, CancellationToken cancellationToken, string officeCode = "3010", string classCode = "A")
    {
        return await WithConnectionAsync(context, async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {Schema}.fn_generate_policy_no(@o, @c)";
            cmd.Parameters.Add(new NpgsqlParameter("o", officeCode));
            cmd.Parameters.Add(new NpgsqlParameter("c", classCode));
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return (string)result!;
        }, cancellationToken);
    }

    public static async Task ProcessBatchValidationAsync(AppDbContext context, long batchId, CancellationToken cancellationToken)
    {
        await ExecuteCallAsync(context, $"CALL {Schema}.sp_process_batch_validation(@p0)",
            new object[] { new NpgsqlParameter("p0", batchId) }, cancellationToken);
    }

    public static async Task AdvanceBatchStatusAsync(AppDbContext context, long batchId, string newStatus, CancellationToken cancellationToken)
    {
        await ExecuteCallAsync(context, $"CALL {Schema}.sp_advance_batch_status(@p0, @p1)",
            new object[] { new NpgsqlParameter("p0", batchId), new NpgsqlParameter("p1", newStatus) },
            cancellationToken);
    }

    public static async Task TagPaymentAsync(AppDbContext context, long proposalId, CancellationToken cancellationToken)
    {
        await ExecuteCallAsync(context, $"CALL {Schema}.sp_tag_payment(@p0)",
            new object[] { new NpgsqlParameter("p0", proposalId) }, cancellationToken);
    }

    // The SGInsurance functions/procedures reference their own tables unqualified, relying on
    // search_path rather than being schema-qualified internally. The default connection search_path
    // is "$user", public, which doesn't include the (mixed-case, quoted) SGInsurance schema. We set it
    // explicitly and keep the same physical connection open across both statements so a pooled
    // connection swap between the SET and the CALL can't undo it.
    private static async Task ExecuteCallAsync(AppDbContext context, string commandText, object[] parameters, CancellationToken cancellationToken)
    {
        var wasClosed = context.Database.GetDbConnection().State != ConnectionState.Open;
        if (wasClosed)
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await context.Database.ExecuteSqlRawAsync("SET search_path TO \"SGInsurance\", public", cancellationToken);
            await context.Database.ExecuteSqlRawAsync(commandText, parameters, cancellationToken);
        }
        finally
        {
            if (wasClosed)
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task<T> WithConnectionAsync<T>(AppDbContext context, Func<NpgsqlConnection, Task<T>> action, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using (var setCmd = connection.CreateCommand())
            {
                setCmd.CommandText = "SET search_path TO \"SGInsurance\", public";
                await setCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            return await action(connection);
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }
}
