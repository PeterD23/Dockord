using Discord.WebSocket;
using Docker.DotNet;
using Docker.DotNet.Models;
using Dockord.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dockord.Docker
{
    public class DockerHandler
    {
        private delegate Task<string> ExecuteCommand(string command, SocketTextChannel callback);

        private DockerClient _client;
        private Dictionary<string, ExecuteCommand> commands;

        private int currentPortAllocation = 0; // Replace this with a iterator range checked against already allocated ports

        public DockerHandler()
        {
            _client = new DockerClientConfiguration().CreateClient();
            _client.DefaultTimeout = TimeSpan.FromSeconds(10);

            commands = new()
            {
                { "/containers", async (x, y) => await GetListOfContainers() },
                { "/restart", RestartContainer },
                { "/stats", async (x, y) => await GetContainerUsageStats(x) },
                { "/build", BuildContainer },
                { "/howdoi", async (x, y) => await UserReference(x) }
            };
        }

        public async Task<string> SendCommand(string command, SocketTextChannel callback)
        {
            var first = command.Split()[0];
            if (commands.ContainsKey(first))
            {
                return await commands[first](RemoveFirstWord(command), callback);
            }
            return "";
        }

        private string RemoveFirstWord(string command)
        {
            if (command.Split().Length == 1)
                return "";
            return command[(command.Split()[0].Length + 1)..].Trim();
        }

        private async Task<string> UserReference(string command)
        {
            return await Task.Run(() =>
            {
                var definition = YamlController.GetGameDefinition(command);
                if (definition != null)
                {
                    return definition.UserReference;
                }
                return "That doesn't appear to be a valid game definition.";
            });
        }

        private async Task<string> BuildContainer(string command, SocketTextChannel callback)
        {
           if (currentPortAllocation == 0)
            {
                var dbValue = await SQLiteController.GetConfigValueFromDB(ConfigKeys.StartPortRange);
                currentPortAllocation = int.Parse(dbValue.ToString());
            }

            var strippedVars = command.Split();
            if (strippedVars.Length == 1)
                return "Cannot build with only 1 parameter.";

            var containerName = strippedVars[1];

            var gameDefinition = YamlController.GetGameDefinition(strippedVars[0]);
            if (gameDefinition == null)
                return "Not a valid game definition.";

            var environmentVars = gameDefinition.RequiredEnvironmentVariables;
            if (strippedVars.Length - 2 < environmentVars.Length)
                return "Not enough parameters supplied. The parameters required are: Image, UniqueName, " + string.Join(", ", environmentVars);

            for (int i = 0; i < environmentVars.Length; i++)
            {
                environmentVars[i] = $"{environmentVars[i]}={strippedVars[i + 2]}";
            }

            var parameters = new CreateContainerParameters()
            {
                Name = containerName,
                Image = gameDefinition.ImageName,
                ExposedPorts = gameDefinition.Ports.ToDictionary(port => port, port => new EmptyStruct()),
                Env = environmentVars,
                Volumes = gameDefinition.Volumes.ToDictionary(volume => volume, volume => new EmptyStruct()),
                HostConfig = new HostConfig()
                {
                    PortBindings = GenerateAvailablePortRanges(gameDefinition.Ports, await SQLiteController.GetPortAllocationsFromDB()),
                    Binds = gameDefinition.Binds,
                }
            };
            var portBindings = ConvertPortBindingsToString(parameters.HostConfig.PortBindings);

            foreach (var folderPath in gameDefinition.FoldersToCreate)
            {
                if (!Directory.Exists($"./ContainerData/{folderPath}"))
                {
                    Directory.CreateDirectory($"./ContainerData/{folderPath}");
                }
            };

            // Pull the image first
            await callback.SendMessageAsync("Pulling image, this may take a while...");
            await _client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = gameDefinition.ImageName,
                Tag = "latest"
            },
            null,
            new Progress<JSONMessage>());
            await callback.SendMessageAsync("Pull complete!");

            // Then create the container, add port bindings to the DB, and run it!
            await callback.SendMessageAsync("Creating container with parameters...");
            var response = await _client.Containers.CreateContainerAsync(parameters);
            SQLiteController.AddValueToPortAllocations(containerName, portBindings);

            await callback.SendMessageAsync("Create complete! Now attempting to start container...");
            var started = await _client.Containers.StartContainerAsync(strippedVars[1], new());

            if (started)
            {
                return $"Success! Container {strippedVars[1]} is up! ID is {response.ID[..4]}. {gameDefinition.PostBuildComment}";
            }
            else
            {
                return "Oh no, something went wrong. I'll put a more descriptive error in here later.";
            }
        }

        private async Task<string> RestartContainer(string command, SocketTextChannel callback)
        {
            await callback.SendMessageAsync($"Attempting to restart container {command}");
            try
            {
                await _client.Containers.RestartContainerAsync(command, new ContainerRestartParameters());
            }
            catch (DockerContainerNotFoundException)
            {
                return $"Container {command} was not found.";
            }
            catch (ArgumentNullException)
            {
                return "Failed due to an invalid argument supplied.";
            }
            catch (DockerApiException)
            {
                return "Failed due to the Docker Daemon returning an error.";
            }
            return $"Container {command} was successfully restarted.";
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

        private async Task<string> GetContainerUsageStats(string command)
        {
            var parameters = new ContainerStatsParameters()
            {
                Stream = false
            };

            var progress = new Collapsable<ContainerStatsResponse>();
            await _client.Containers.GetContainerStatsAsync(command, parameters, progress);
            var final = progress.ReturnValue();

            // https://docs.docker.com/engine/api/v1.41/#tag/Container/operation/ContainerStats
            double used_memory = final.MemoryStats.Usage - final.MemoryStats.Stats["cache"];
            double available_memory = final.MemoryStats.Limit;
            double memory_usage = (used_memory / available_memory) * 100.0;
            double cpu_delta = final.CPUStats.CPUUsage.TotalUsage - final.PreCPUStats.CPUUsage.TotalUsage;
            double system_cpu_delta = final.CPUStats.SystemUsage - final.PreCPUStats.SystemUsage;
            double cpu_usage = (cpu_delta / system_cpu_delta) * final.CPUStats.OnlineCPUs * 100.0;

            var header = string.Format($"{"CPU %",-10}{"MEM USAGE / LIMIT (GiB)",-30}{"MEM %",-10}");
            var breakdown = string.Format($"{cpu_usage,-10:f2}{used_memory / 1000000000,-10:f2}/ {available_memory / 1000000000,-20:f2}{memory_usage,-10:f2}");

            return $"```{header}\n{breakdown}```";
        }

        private static string GenerateReadableStringFromContainer(ContainerListResponse container)
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

        private IDictionary<string, IList<PortBinding>> GenerateAvailablePortRanges(string[] portRanges, List<string> existingPorts)
        {
            var portAllocations = new List<int>();
            var portBindings = new Dictionary<string, IList<PortBinding>>();
            foreach (var portRange in portRanges)
            {
                var split = portRange.Split("/");
                var protocol = split.Length > 1 ? $"/{split[1]}" : "";

                // This will generate a range of ports from X-Y, unless there is no range (arr length = 1) in which case it just creates a range of 1
                var range = Array.ConvertAll(portRange.Split("/")[0].Split("-"), int.Parse);
                var numericPorts = Enumerable.Range(range[0], range.Length > 1 ? (range[1] - range[0]) + 1 : 1).ToList();

                foreach (var num in numericPorts)
                {
                    var bindingPort = GetNextAvailablePort(existingPorts);
                    currentPortAllocation += 1;

                    var binding = new List<PortBinding>
                    {
                        new PortBinding
                        {
                            HostPort = $"{bindingPort}"
                        }
                    };
                    portBindings.Add($"{num}{protocol}", binding);
                }
            }
            return portBindings;
        }

        private int GetNextAvailablePort(List<string> existingPorts)
        {
            while (existingPorts.Contains(currentPortAllocation.ToString()))
                currentPortAllocation += 1;
            return currentPortAllocation;
        }

        private string ConvertPortBindingsToString(IDictionary<string, IList<PortBinding>> portBindings)
        {
            var assignedPorts = new List<string>();
            foreach (var bind in portBindings)
            {
                foreach (var port in bind.Value)
                {
                    assignedPorts.Add(port.HostPort);
                }
            }
            return string.Join(",", assignedPorts);
        }
    }
}
