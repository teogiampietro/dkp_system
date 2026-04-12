using System.Data;
using Npgsql;
using Dapper;
using DkpSystem.Models;

namespace DkpSystem.Data;

/// <summary>
/// Factory for creating database connections to PostgreSQL.
/// </summary>
public class DbConnectionFactory
{
    private readonly string _connectionString;
    private static bool _mappingConfigured = false;
    private static readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConnectionFactory"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        ConfigureColumnMapping();
    }

    /// <summary>
    /// Configures Dapper column name mapping for snake_case to PascalCase.
    /// </summary>
    private static void ConfigureColumnMapping()
    {
        if (_mappingConfigured) return;
        
        lock (_lock)
        {
            if (_mappingConfigured) return;

            // Configure custom type map for all entities to handle snake_case column names
            var typesToMap = new[]
            {
                typeof(User),
                typeof(Guild),
                typeof(Event),
                typeof(EventRewardLine),
                typeof(DkpEarning),
                typeof(Auction),
                typeof(AuctionItem),
                typeof(AuctionBid)
            };

            foreach (var type in typesToMap)
            {
                SqlMapper.SetTypeMap(
                    type,
                    new CustomPropertyTypeMap(
                        type,
                        (t, columnName) =>
                        {
                            // Convert snake_case to PascalCase
                            var propertyName = string.Join("", columnName.Split('_')
                                .Select(s => char.ToUpper(s[0]) + s.Substring(1)));
                            return t.GetProperty(propertyName);
                        }
                    )
                );
            }

            _mappingConfigured = true;
        }
    }

    /// <summary>
    /// Creates and opens a new database connection.
    /// </summary>
    /// <returns>An open database connection.</returns>
    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
