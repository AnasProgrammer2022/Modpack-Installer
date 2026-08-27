using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Modpack_Installer.Definitions.JsonRequestDefinitions
{
    /// <summary>
    /// This provides classes for reading the project's versions
    /// </summary>

    public class hashes
    {
        public string sha512 { set; get; } = string.Empty;
        public string sha1 { set; get; } = string.Empty;
    }

    public class filedata
    {
        public string id { set; get; } = string.Empty;
        public hashes hashes { set; get; } = new hashes();
        public string url { set; get; } = string.Empty;
        public string filename { set; get; } = string.Empty;
        //public bool primary { set; get; } = false;
        public int size { set; get; } = 0;
        //public string file_type { set; get; } = string.Empty;
    }

    public class dependency
    {
        public string version_id { set; get; } = string.Empty;
        public string project_id { set; get; } = string.Empty;
        public string file_name { set; get; } = string.Empty;
        public string dependency_type { set; get; } = string.Empty;
    }

    public class VersionData
    {
        public List<string> game_versions { set; get; } = new List<string>();
        public List<string> loaders { set; get; } = new List<string>();
        public string id { set; get; } = string.Empty;
        public string project_id { set; get; } = string.Empty;
        //public string author_id { set; get; } = string.Empty;
        //public bool featured { set; get; } = false;
        public string name { set; get; } = string.Empty;
        public string version_number { set; get; } = string.Empty;
        public string changelog { set; get; } = string.Empty;
        //public string changelog_url { set; get; } = string.Empty;
        public string date_published { set; get; } = string.Empty;
        //public int downloads { set; get; } = 0;
        public string version_type { set; get; } = string.Empty;
        //public string status { set; get; } = string.Empty;
        //public string request_status { set; get; } = string.Empty;
        public List<filedata> files { set; get; } = new List<filedata>();
        public List<dependency> dependencies { set; get; } = new List<dependency>();
        public string related_project_title = string.Empty;
        public string related_project_description = string.Empty;
    }
}
