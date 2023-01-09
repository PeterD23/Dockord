using Discord;
using Dockord.Discord;
using Dockord.Docker;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dockord.Core
{
    public class Program
    {
        private DiscordHandler DiscordHandler;
        private DockerHandler DockerHandler;

        public static Task Main() => new Program().MainAsync();

        public async Task MainAsync()
        {
            if (!File.Exists("dockord.db"))
            {
               SQLiteController.CreateNewDB();
            }

            DiscordHandler = new DiscordHandler();
            await DiscordHandler.RunBot();

            DockerHandler = new DockerHandler();
            DiscordHandler.AddDocker(DockerHandler);

            await Task.Delay(-1);
        }


    }
}
