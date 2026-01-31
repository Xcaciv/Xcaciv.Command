using System;
using System.Collections.Generic;
using System.Text;

namespace Xcaciv.Command.FileLoader
{
    public class EnvironmentFile
    {
        public Dictionary<string, string> Global { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> CommandEnvironments { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    }
}
