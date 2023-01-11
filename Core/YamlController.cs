using Dockord.Docker;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dockord.Core
{
    public class YamlController
    {
        private static string FileLocation = "./Defs/";

        public static GameDefinition GetGameDefinition(string gameName)
        {
            if (!File.Exists($"{FileLocation}{gameName}.yml"))
                return null;

            var deserializer = new DeserializerBuilder().WithNamingConvention(new HyphenatedNamingConvention()).Build();
            var definition = deserializer.Deserialize<GameDefinition>(File.ReadAllText($"{FileLocation}{gameName}.yml"));

            definition.AddWorkingDir();

            return definition;
        }

    }
}
