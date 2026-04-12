using Npgsql;

namespace DkpSystem.Data;

public class DatabaseMigrator
{
    private readonly string _connectionString;

    public DatabaseMigrator(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task RunMigrationsAsync()
    {
        Console.WriteLine("🔄 Running database migrations...");
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Create migrations tracking table if it doesn't exist
        await EnsureMigrationsTableExistsAsync(connection);

        // Execute migrations in order (only if not already executed)
        await ExecuteMigrationAsync(connection, "001_initial_schema.sql");
        await ExecuteMigrationAsync(connection, "002_seed_guild.sql");
        await ExecuteMigrationAsync(connection, "003_seed_admin.sql");
        
        Console.WriteLine("✅ Database migrations completed successfully!");
    }

    private async Task EnsureMigrationsTableExistsAsync(NpgsqlConnection connection)
    {
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                id SERIAL PRIMARY KEY,
                migration_name VARCHAR(255) NOT NULL UNIQUE,
                executed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );";
        
        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<bool> IsMigrationExecutedAsync(NpgsqlConnection connection, string migrationName)
    {
        var checkSql = "SELECT COUNT(*) FROM __migrations WHERE migration_name = @name";
        await using var command = new NpgsqlCommand(checkSql, connection);
        command.Parameters.AddWithValue("name", migrationName);
        
        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return count > 0;
    }

    private async Task MarkMigrationAsExecutedAsync(NpgsqlConnection connection, string migrationName)
    {
        var insertSql = "INSERT INTO __migrations (migration_name) VALUES (@name) ON CONFLICT (migration_name) DO NOTHING";
        await using var command = new NpgsqlCommand(insertSql, connection);
        command.Parameters.AddWithValue("name", migrationName);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ExecuteMigrationAsync(NpgsqlConnection connection, string fileName)
    {
        try
        {
            // Check if migration was already executed
            if (await IsMigrationExecutedAsync(connection, fileName))
            {
                Console.WriteLine($"  ⏭️  {fileName} (already executed)");
                return;
            }

            var filePath = Path.Combine("Migrations", fileName);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"⚠️  Migration file not found: {fileName}");
                return;
            }

            var script = await File.ReadAllTextAsync(filePath);
            
            await using var command = new NpgsqlCommand(script, connection);
            await command.ExecuteNonQueryAsync();
            
            // Mark migration as executed
            await MarkMigrationAsExecutedAsync(connection, fileName);
            
            Console.WriteLine($"  ✓ {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️  {fileName}: {ex.Message}");
        }
    }
}
