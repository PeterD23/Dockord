using Discord;
using Discord.WebSocket;
using Dockord.Core;
using Dockord.Docker;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dockord.Discord
{
    public class DiscordHandler
    {
        private DiscordSocketClient _client;

        private ulong logChannel = 0L;
        private bool failedToGetLogChannel = false;

        private ulong approvedRole = 0L;

        private delegate Task<string> DockerTrigger(string command, SocketTextChannel channel);
        private DockerTrigger SendCommand;

        public DiscordHandler()
        {
            var intentConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            };
            _client = new DiscordSocketClient(intentConfig);
            _client.Log += Log;
        }

        public async Task RunBot()
        {
            if (!File.Exists("token"))
            {
                throw new FileNotFoundException("Discord Token could not be found.");
            }
            var token = File.ReadAllText("token");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await SetApprovedRoleFromDB();

            _client.MessageReceived += MessageReceived;
        }

        public void AddDocker(DockerHandler docker)
        {
            SendCommand = docker.SendCommand;
        }

        private async Task Log(LogMessage msg)
        {
            Console.WriteLine(msg);

            // Log to Discord only if connected, and if the log channel is configured correctly
            if (_client.ConnectionState != ConnectionState.Connected || failedToGetLogChannel)
                return;
            if (logChannel == 0L)
            {
                var data = await SQLiteController.GetConfigValueFromDB(ConfigKeys.LoggingChannelId);
                if (data.GetType() != typeof(DBNull))
                {
                    logChannel = Convert.ToUInt64(data);
                }
                else
                {
                    failedToGetLogChannel = true;
                    return;
                }
            }
            var log = await _client.GetChannelAsync(logChannel) as SocketTextChannel;
            await log.SendMessageAsync(msg.ToString());
        }

        private Task MessageReceived(SocketMessage message)
        {
            Task.Run(async () =>
            {
                if (message.Author.IsBot) return;

                var messageContent = message.Content;

                if (!RoleMatchesApproved(message.Author) || !messageContent.StartsWith("/")) return;

                await Log(new LogMessage(LogSeverity.Info, "Discord", "Approved User is attempting a command, " + messageContent));
                var channel = message.Channel as SocketTextChannel;

                var response = await SendCommand(messageContent, channel);
                if (!response.Equals(""))
                {
                    await channel.SendMessageAsync(response);
                }
            });
            return Task.CompletedTask;
        }

        private bool RoleMatchesApproved(SocketUser user)
        {
            var guildUser = user as SocketGuildUser;
            return guildUser.Roles.Any(role => role.Id.Equals(approvedRole));
        }

        private async Task SetApprovedRoleFromDB()
        {
            var data = await SQLiteController.GetConfigValueFromDB(ConfigKeys.ApproverRoleId);
            if (data.GetType() != typeof(DBNull))
            {
                approvedRole = Convert.ToUInt64(data);
            }
        }

    }
}
