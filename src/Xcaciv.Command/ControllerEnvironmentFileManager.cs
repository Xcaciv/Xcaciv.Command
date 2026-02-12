using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command
{
    public class ControllerEnvironmentFileManager: EnvironmentFileManager
    {
        public ControllerEnvironmentFileManager(IFileSystem? fileSystem = null) : base(fileSystem)
        {
        }

        public IControllerEnvironmentContext LoadControllerEnvironmentFromFile(string filePath)
        {
            var envFile = LoadEnvironmentFile(filePath);
            var env = new ControllerEnvironmentContext(new EnvironmentContext(envFile.Global));
            foreach (var (key, item) in envFile.CommandEnvironments)
            {
                env.UpdateEnvironment(item, key);
            }
            return env;
        }
    }
}
