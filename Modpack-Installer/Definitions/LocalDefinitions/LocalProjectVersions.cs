using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modpack_Installer.Definitions.LocalDefinitions
{
    public class Version
    {
        public int Major { get; set; } = 0;
        public int Minor { get; set; } = 0;
        public int Patch { get; set; } = 0;
        public string attribute { get; set; } = string.Empty;
    }

    public enum Loader
    {
        Minecraft,
        Fabric,
        Forge,
        Quilt,
        NeoForge
    }

    public class ProjectVersion
    {
        public string title { get; set; } = string.Empty;
        public string versionName { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string projectID { get; set; } = string.Empty;
        public string versionID { get; set; } = string.Empty;
        public string filePath { get; set; } = string.Empty;
        public string versionType { get; set; } = string.Empty;
        public string shouldReplaceConfigs { get; set; } = string.Empty;
        public List<string> configFiles { get; set; } = new List<string>();
        public List<string> mcVersions { get; set; } = new List<string>();
        public List<string> loaders { get; set; } = new List<string>();
        public List<string> dependenciesProjectsID { get; set; } = new List<string>();
    }
}
