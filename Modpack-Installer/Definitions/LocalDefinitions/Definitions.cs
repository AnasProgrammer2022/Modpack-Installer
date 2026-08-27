using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modpack_Installer.Definitions.LocalDefinitions
{
    public enum LogState
    {
        Error,
        Warning,
        Info
    }

    internal class LogLine
    {
        public bool IsSuccessful { get; set; }
        public LogState LogLineState { get; set; }
        public string Data { get; set; } = string.Empty;
    }
}
