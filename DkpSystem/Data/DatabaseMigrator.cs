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

        // Execute migrations in order
        await ExecuteMigrationAsync(connection, "001_initial_schema.sql");
        await ExecuteMigrationAsync(connection, "002_seed_guild.sql");
        await ExecuteMigrationAsync(connection, "003_seed_admin.sql");
        
        Console.WriteLine("✅ Database migrations completed successfully!");
    }

    private async Task ExecuteMigrationAsync(NpgsqlConnection connection, string fileName)
    {
        try
        {
            var filePath = Path.Combine("Migrations", fileName);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"⚠️  Migration file not found: {fileName}");
                return;
            }

            var script = await File.ReadAllTextAsync(filePath);
            
            await using var command = new NpgsqlCommand(script, connection);
            await command.ExecuteNonQueryAsync();
            
            Console.WriteLine($"  ✓ {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️  {fileName}: {ex.Message}");
        }
    }
}
