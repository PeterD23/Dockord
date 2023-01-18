using System;
using YamlDotNet.Serialization;

namespace Dockord.Docker
{
    public class GameDefinition
    {
        [YamlMember(Alias = "help")]
        public string UserReference { get; set; }

        [YamlMember(Alias = "image-name")]
        public string ImageName { get; set; }

        [YamlMember(Alias = "folders")]
        public string[] FoldersToCreate { get; set; }

        [YamlMember(Alias = "budget")]
        public double RamBudget { get; set; }

        [YamlMember(Alias = "ports")]
        public string[] Ports { get; set; }

        [YamlMember(Alias = "environment")]
        public string[] RequiredEnvironmentVariables {  get; set; }

        [YamlMember(Alias = "optional-env")]
        public string[] OptionalEnvironmentVariables { get; set; }

        [YamlMember(Alias = "volumes")]
        public string[] Volumes { get; set; }

        [YamlMember(Alias = "binds")]
        public string[] Binds {  get; set; }


        [YamlMember(Alias = "post-build-comment")]
        public string PostBuildComment { get; set; }

        public void AddWorkingDir()
        {
            if(Binds == null) 
                return;

            for (int i = 0; i < Binds.Length; i++)
            {
                Binds[i] = AppContext.BaseDirectory.Replace("\\","/") + Binds[i];
            }
        }
    }  
}
