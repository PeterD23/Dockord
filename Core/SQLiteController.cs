using CliWrap;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace Dockord.Core
{
    public enum ConfigKeys
    {
        LoggingChannelId, ApproverChannelId, ApproverRoleId, StartPortRange
    }

    public class SQLiteController
    {

        /** Initialise Dockord.db with the following tables:
         *  config: Keys for configuration values
         *  container_discord_mappings: Mapping table to associate a docker container with a discord channel and leader role
         *  port_allocations: Which ports are allocated to which container
         */

        public static async void CreateDefaultDB()
        {
            var connection = new SqliteConnection("Data Source=dockord.db");
            await connection.OpenAsync();

            var createConfigSql = @"CREATE TABLE IF NOT EXISTS main.config(
                                        config_key varchar(50) PRIMARY KEY NOT NULL, 
                                        config_value varchar(50), 
                                        UNIQUE(config_key, config_value)
                                    );";
            var command = new SqliteCommand(createConfigSql, connection);
            await command.ExecuteNonQueryAsync();

            var addConfigKeysSql = $@"INSERT OR IGNORE INTO 'config'(
                                        'config_key', 'config_value') 
                                        VALUES 
                                            ('{nameof(ConfigKeys.LoggingChannelId)}', NULL),
                                            ('{nameof(ConfigKeys.ApproverChannelId)}', NULL),
                                            ('{nameof(ConfigKeys.ApproverRoleId)}', NULL),
                                            ('{nameof(ConfigKeys.StartPortRange)}', 5000);";
            command = new SqliteCommand(addConfigKeysSql, connection);
            await command.ExecuteNonQueryAsync();

            var createMappingsSql = @"CREATE TABLE IF NOT EXISTS main.container_discord_mappings(
                                        container_name varchar(50) PRIMARY KEY NOT NULL,
                                        discord_channel_id INTEGER, 
                                        discord_leader_role_id INTEGER,
                                        UNIQUE(container_name, discord_channel_id, discord_leader_role_id)
                                    );";
            command = new SqliteCommand(createMappingsSql, connection);
            await command.ExecuteNonQueryAsync();

            var createPortAllocationSql = @"CREATE TABLE IF NOT EXISTS main.port_allocations(
                                                container_name varchar(50) PRIMARY KEY NOT NULL, 
                                                ports_allocated varchar(255),
                                                UNIQUE(container_name, ports_allocated)
                                            );";
            command = new SqliteCommand(createPortAllocationSql, connection);
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

        public static async Task<List<string>> GetPortAllocationsFromDB()
        {
            var connection = new SqliteConnection("Data Source=dockord.db");
            await connection.OpenAsync();

            var getPortsAllocatedSql = $"SELECT ports_allocated FROM port_allocations";
            var command = new SqliteCommand(getPortsAllocatedSql, connection);

            var ports = "";
            var reader = await command.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    ports += reader.GetString(0) + ",";
                }
            }
            await connection.CloseAsync();
            return ports.Split(",").ToList();
        }

        public static async void RemoveValueFromPortAllocations(string container_name)
        {
            var connection = new SqliteConnection("Data Source=dockord.db");
            await connection.OpenAsync();

            var removePortsAllocatedSql = $"DELETE FROM 'port_allocations' WHERE container_name = '{container_name}';";
            var command = new SqliteCommand(removePortsAllocatedSql, connection);
            await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();
        }

        public static async void AddValueToPortAllocations(string container_name, string ports_allocated)
        {
            var connection = new SqliteConnection("Data Source=dockord.db");
            await connection.OpenAsync();

            var addPortsAllocatedSql = $"INSERT OR IGNORE INTO 'port_allocations'('container_name', 'ports_allocated') VALUES ('{container_name}', '{ports_allocated}');";
            var command = new SqliteCommand(addPortsAllocatedSql, connection);
            await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();
        }

    }
}
