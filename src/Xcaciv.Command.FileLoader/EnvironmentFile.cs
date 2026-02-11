using System;
using System.Collections.Generic;
using System.Text;

namespace Xcaciv.Command.FileLoader
{
    /// <summary>
    /// Data transfer object used for YAML serialization and deserialization of environment
    /// configuration, including both global environment variables and command-specific
    /// environment variable sets.
    /// </summary>
    public class EnvironmentFile
    {
        public Dictionary<string, string> Global { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> CommandEnvironments { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    }
}
