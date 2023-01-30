using Dockord.Discord;
using Dockord.Docker;
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
               SQLiteController.CreateDefaultDB();
            }

            if (!Directory.Exists("ContainerData"))
            {
                Directory.CreateDirectory("ContainerData");
            }

            DiscordHandler = new DiscordHandler();
            await DiscordHandler.RunBot();

            DockerHandler = new DockerHandler();
            DiscordHandler.AddDocker(DockerHandler);

            await Task.Delay(-1);
        }
    }
}
