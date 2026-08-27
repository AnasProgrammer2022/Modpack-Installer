using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modpack_Installer.Definitions.OnlineDefinitions
{
    public class SearchHit
    {
        public string project_id { set; get; } = string.Empty;
        public string project_type { set; get; } = string.Empty;
        public List<string> all_project_types { set; get; } = new List<string>();
        public string slug { set; get; } = string.Empty;
        public string author { set; get; } = string.Empty;
        public string author_id { set; get; } = string.Empty;
        public string title { set; get; } = string.Empty;
        public string description { set; get; } = string.Empty;
        public List<string> categories { set; get; } = new List<string>();
        public List<string> versions { set; get; } = new List<string>();
        public int downloads { set; get; } = 0;
        public int follows { set; get; } = 0;
        public int? color { set; get; } = 0;
    }

    internal class SearchData
    {
        public List<SearchHit> hits { set; get; } = new List<SearchHit>();
        public int limit { set; get; } = 0;
        public int total_hits { set; get; } = 0;
    }
}
