using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using Xcaciv.Command.Interface;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Xcaciv.Command.FileLoader
{
    /// <summary>
    /// a class for managing environment configuration from a file
    /// directly loads and saves environment serialized in YAML format
    /// </summary>
    public class EnvironmentFileManager
    {
        private IFileSystem fileSystem;

        public EnvironmentFileManager(IFileSystem? fileSystem = null)
        {
            this.fileSystem = fileSystem ?? new FileSystem();
        }
        /// <summary>
        /// Saves the current environment state to a specified file.
        /// </summary>
        /// <remarks>This method creates an <see cref="EnvironmentFile"/> object populated with the
        /// current environment data before saving it to the specified file.</remarks>
        /// <param name="filePath">The path to the file where the environment data will be saved. This value must not be null or empty.</param>
        /// <param name="environment">An instance of <see cref="IEnvironmentContext"/> that provides the environment data to be saved.</param>
        /// <returns>true if the environment is successfully saved; otherwise, false.</returns>
        /// <exception cref="Exception">Thrown if an error occurs while saving the environment to the specified file.</exception>
        public bool SaveEnvironment(string filePath, IEnvironmentContext environment)
        {
            var envFile = new EnvironmentFile();
            try
            {
                // populate envFile from controllerEnvironment
                envFile.Global = environment.GetEnvironment();
                SaveEnvironmentFile(filePath, envFile);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save environment to file '{filePath}'.", ex);
            }
        }

        /// <summary>
        /// Saves the current environment state to a specified file in YAML format.
        /// </summary>
        /// <remarks>This method serializes the environment data into YAML format and writes it to the
        /// specified file. Ensure that the file path is accessible and writable.</remarks>
        /// <param name="filePath">The path to the file where the environment data will be saved. Must be a valid file path.</param>
        /// <param name="controllerEnvironment">An instance of IControllerEnvironmentContext that provides the environment data to be saved.</param>
        /// <returns>true if the environment was successfully saved; otherwise, false.</returns>
        /// <exception cref="Exception">Thrown if an error occurs while saving the environment to the specified file.</exception>
        public bool SaveEnvironment(string filePath, IControllerEnvironmentContext controllerEnvironment)
        {
            var envFile = new EnvironmentFile();
            try
            {
                // populate envFile from controllerEnvironment
                envFile.Global = controllerEnvironment.GetEnvironment();
                foreach (var command in controllerEnvironment.GetCommandEnvironmentNames())
                {
                    var commandEnv = controllerEnvironment.GetEnvironment(command);
                    envFile.CommandEnvironments.Add(command, commandEnv);
                }

                SaveEnvironmentFile(filePath, envFile);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save environment to file '{filePath}'.", ex);
            }
        }

        /// <summary>
        /// Saves the specified environment file to the given file path in YAML format.
        /// </summary>
        /// <remarks>The method serializes the provided environment file to YAML format and writes it to
        /// the specified file path. It ensures that the file name ends with a '.yaml' extension.</remarks>
        /// <param name="filePath">The path where the environment file will be saved. The path must be valid and should not be null. If the
        /// path does not end with '.yaml' or '.yml', '.yaml' will be appended automatically.</param>
        /// <param name="envFile">The environment file object to be serialized and saved. This object contains the configuration settings that
        /// need to be persisted.</param>
        private void SaveEnvironmentFile(string filePath, EnvironmentFile envFile)
        {
            // serialize to YAML and write to file
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(envFile);

            // get absolute path
            var absolutePath = fileSystem.Path.GetFullPath(filePath);
            // ensure file name ends with .yaml or .yml
            if (!absolutePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) &&
                !absolutePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                absolutePath += ".yaml";
            }

            fileSystem.File.WriteAllText(absolutePath, yaml, Encoding.UTF8);
        }

        /// <summary>
        /// Retrieves the environment context associated with the specified file path.
        /// </summary>
        /// <remarks>Ensure that the provided file path points to a valid file; otherwise, an exception
        /// may be thrown.</remarks>
        /// <param name="filePath">The path to the file for which the environment context is to be retrieved. This parameter cannot be null or
        /// empty.</param>
        /// <returns>An instance of <see cref="IControllerEnvironmentContext"/> that represents the environment context for the
        /// specified file path.</returns>
        public EnvironmentFile LoadEnvironmentFile(string filePath)
        {
            var envFile = new EnvironmentFile();
            try
            {
                // get absolute path
                var absolutePath = fileSystem.Path.GetFullPath(filePath);
                // ensure file name ends with .yaml or .yml
                if (!absolutePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) &&
                    !absolutePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                {
                    absolutePath += ".yaml";
                }
                var yaml = fileSystem.File.ReadAllText(absolutePath, Encoding.UTF8);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                envFile = deserializer.Deserialize<EnvironmentFile>(yaml) ?? new EnvironmentFile();

                return envFile;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load environment from file '{filePath}'.", ex);
            }
        }
    }
}
