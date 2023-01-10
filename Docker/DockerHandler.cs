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
            var parameters = new ContainersListParameters();
            var containers = await _client.Containers.ListContainersAsync(parameters);

            var response = string.Format($"{"ID",-6}{"Name",-25}{"State",-9}{"Status",-15}{"Port(s)",-20}");

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
                var ip = port.IP != null ? port.IP + ":" : "";
                var publicPort = port.PublicPort != 0 ? port.PublicPort + "->" : "";
                portData += $"{ip}{publicPort}{port.PrivatePort}/{port.Type} ";
            }
            return string.Format($"{container.ID[..4],-6}{string.Join(",", container.Names),-25}{container.State,-9}{container.Status,-15}{portData,-20}");
        }

    }
}
