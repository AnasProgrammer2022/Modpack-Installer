using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modpack_Installer.Definitions.JsonRequestDefinitions
{
    public class license
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
    }

    public class moderator_message
    {
        public string message { get; set; } = string.Empty;
        public string body { get; set; } = string.Empty;
    }

    public class gallery
    {
        public string url { get; set; } = string.Empty;
        public bool featured { get; set; } = false;
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string created { get; set; } = string.Empty;
        public int ordering { get; set; } = 0;
    }

    public class donation_urls
    {
        public string id { get; set; } = string.Empty;
        public string platform { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
    }

    public class ProjectData
    {
        //public string client_side { get; set; } = string.Empty;
        //public string server_side { get; set; } = string.Empty;
        public List<string> game_versions { get; set; } = new List<string>();
        public List<string> environment { get; set; } = new List<string>();
        public string id { get; set; } = string.Empty;
        public string slug { get; set; } = string.Empty;
        public string project_type { get; set; } = string.Empty;
        //public string team { get; set; } = string.Empty;
        //public string organization { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        //public string body { get; set; } = string.Empty;
        //public string body_url { get; set; } = string.Empty;
        //public string published { get; set; } = string.Empty;
        //public string updated { get; set; } = string.Empty;
        //public string approved { get; set; } = string.Empty;
        //public string queued { get; set; } = string.Empty;
        //public string status { get; set; } = string.Empty;
        //public string requested_status { get; set; } = string.Empty;
        //public moderator_message moderator_message { get; set; } = new moderator_message();
        //public license license { get; set; } = new license();
        //public int downloads { get; set; } = 0;
        //public int followers { get; set; } = 0;
        //public List<string> categories { get; set; } = new List<string>();
        //public List<string> additional_categories { get; set; } = new List<string>();
        public List<string> loaders { get; set; } = new List<string>();
        public List<string> versions { get; set; } = new List<string>();
        //public string icon_url { get; set; } = string.Empty;
        //public string issues_url { get; set; } = string.Empty;
        //public string source_url { get; set; } = string.Empty;
        //public string wiki_url { get; set; } = string.Empty;
        //public string discord_url { get; set; } = string.Empty;
        //public List<donation_urls> donation_urls { get; set; } = new List<donation_urls>();
        //public List<gallery> gallery { get; set; } = new List<gallery>();
    }
}
