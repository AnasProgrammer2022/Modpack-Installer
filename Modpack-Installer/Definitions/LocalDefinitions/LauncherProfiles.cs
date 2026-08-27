using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modpack_Installer.Definitions.LocalDefinitions
{
    /// <summary>
    /// Provides a Minecraft profile data, such as the name, the game directory, the game version... etc.
    /// </summary>
    public class Profile
    {
        public string name { get; set; } = string.Empty;
        public string gameDir { get; set; } = string.Empty;
        public string lastVersionId { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string mcVersion { get; set; } = string.Empty;
        public string loader { get; set; } = string.Empty;
        public string id = string.Empty;
    }

    /// <summary>
    /// Provieds a list of Minecraft profiles which contain the data
    /// </summary>
    public class LauncherProfiles
    {
        public Dictionary<string, Profile> profiles { get; set; } = new Dictionary<string, Profile>();
    }
}
