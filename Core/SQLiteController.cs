using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace Dockord.Core
{
    public enum ConfigKeys
    {
        LoggingChannelId, ApproverChannelId, ApproverRoleId
    }

    public class SQLiteController
    {

        /** Initialise Dockord.db with the following tables:
         *  config: Keys for configuration values
         *  container_discord_mappings: Mapping table to associate a docker container with a discord channel and leader role
         */

        public static async void CreateNewDB()
        {
            var connection = new SqliteConnection("Data Source=dockord.db");
            await connection.OpenAsync();

            var createConfigSql = "CREATE TABLE IF NOT EXISTS main.config (config_key varchar(50) PRIMARY KEY NOT NULL, config_value varchar(50));";
            var command = new SqliteCommand(createConfigSql, connection);
            await command.ExecuteNonQueryAsync();

            var addConfigKeysSql = $"INSERT INTO 'config' ('config_key', 'config_value') VALUES ('{nameof(ConfigKeys.LoggingChannelId)}', NULL), ('{nameof(ConfigKeys.ApproverChannelId)}', NULL), ('{nameof(ConfigKeys.ApproverRoleId)}', NULL);";
            command = new SqliteCommand(addConfigKeysSql, connection);
            await command.ExecuteNonQueryAsync();

            var createMappingsSql = "CREATE TABLE IF NOT EXISTS main.container_discord_mappings (container_name varchar(50) PRIMARY KEY NOT NULL, discord_channel_id INTEGER, discord_leader_role_id INTEGER);";
            command = new SqliteCommand(createMappingsSql, connection);
            await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();
        }
        
        public static async Task<object> GetConfigValueFromDB(ConfigKeys key)
        {
            var connection = new SqliteConnection("Data Source=dockord.db");
            await connection.OpenAsync();

            var getConfigValueSql = $"SELECT config_value FROM config WHERE config_key = '{key}'";
            var command = new SqliteCommand(getConfigValueSql, connection);

            return await command.ExecuteScalarAsync();
        }

    }
}
