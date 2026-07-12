using System.Security.Cryptography;
using Npgsql;

namespace BcgHub.Api.Infrastructure;

public interface IEmailSyncLock
{
    Task<IAsyncDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class PostgresEmailSyncLock(IConfiguration configuration) : IEmailSyncLock
{
    public async Task<IAsyncDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection is missing.");
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var key = BitConverter.ToInt64(SHA256.HashData(userId.ToByteArray()), 0);
        await using var command = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection);
        command.Parameters.AddWithValue("key", key);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return new AdvisoryLockHandle(connection, key);
    }

    private sealed class AdvisoryLockHandle(NpgsqlConnection connection, long key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection);
                command.Parameters.AddWithValue("key", key);
                await command.ExecuteNonQueryAsync();
            }
            finally { await connection.DisposeAsync(); }
        }
    }
}
