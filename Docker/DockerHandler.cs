using Docker.DotNet;
using Docker.DotNet.Models;
using System.Threading.Tasks;

namespace Dockord.Docker
{
    public class DockerHandler
    {
        private DockerClient _client;

        public DockerHandler()
        {
            _client = new DockerClientConfiguration().CreateClient();
        }

        public async Task<string> SendCommand(string command)
        {
            if (command == "!containers")
            {
                return await GetListOfContainers();
            }
            return "";
        }

        private async Task<string> GetListOfContainers()
        {
            var parameters = new ContainersListParameters() { All = true };
            var containers = await _client.Containers.ListContainersAsync(parameters);

            var response = $"ID     Name\t\tState\t\tStatus\t\tPort(s)";

            if (containers.Count > 0)
            {
                foreach (var container in containers)
                {
                    response += "\n" + GenerateReadableStringFromContainer(container);
                }
                return $"```{response}```";
            }
            else
                return "There are no currently running containers.";
        }

        private string GenerateReadableStringFromContainer(ContainerListResponse container)
        {
            var portData = "";
            foreach (var port in container.Ports)
            {
                var ip = port.IP.Length > 0 ? port.IP + ":" : "";
                var publicPort = port.PublicPort != 0 ? port.PublicPort + "->" : "";
                portData += $"{ip}:{publicPort}->{port.PrivatePort}/{port.Type} ";
            }
            return $"{container.ID[..4]}     {string.Join(",", container.Names)}\t\t{container.State}\t\t{container.Status}\t\t{portData}";
        }

    }
}
