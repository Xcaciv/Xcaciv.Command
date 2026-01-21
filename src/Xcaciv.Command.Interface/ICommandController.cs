namespace Xcaciv.Command.Interface
{
    public interface ICommandController
    {
        /// <summary>
        /// Register built-in commands in the command registry.
        /// This is the primary method for enabling the framework's default command set.
        /// </summary>
        void RegisterBuiltInCommands();
        
        /// <summary>
        /// add a directory from which to load commands
        /// </summary>
        /// <param name="directory"></param>
        void AddPackageDirectory(string directory);
        /// <summary>
        /// load commands from the pacakge driectory with a sub directory filter
        /// </summary>
        /// <param name="subDirectory"></param>
        void LoadCommands(string subDirectory = "bin");
        /// <summary>
        /// run a command using the loaded command pacakges
        /// </summary>
        /// <param name="commandLine"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        Task Run(string commandLine, IIoContext output, IControllerEnvironmentContext env);
        /// <summary>
        /// run a command using the loaded command packages with cancellation support
        /// </summary>
        /// <param name="commandLine">Command line to execute</param>
        /// <param name="output">IO context</param>
        /// <param name="env">Environment context</param>
        /// <param name="cancellationToken">Cancellation token to cancel execution</param>
        Task Run(string commandLine, IIoContext output, IControllerEnvironmentContext env, CancellationToken cancellationToken);
        /// <summary>
        /// install a single command into the index
        /// </summary>
        /// <param name="command"></param>
        void AddCommand(ICommandDescription command);
        /// <summary>
        /// add a command from a loaded type
        /// good for  commands from internal or linked dlls
        /// </summary>
        /// <param name="packageKey"></param>
        /// <param name="commandType"></param>
        /// <param name="modifiesEnvironment"></param>
        void AddCommand(string packageKey, Type commandType, bool modifiesEnvironment = false);
        /// <summary>
        /// Registers a command delegate for the specified package key, optionally indicating whether the command
        /// modifies the environment.
        /// </summary>
        /// <remarks>If multiple commands are registered for the same package key, only the most recently
        /// added command will be used. Commands that modify the environment may have side effects that persist beyond
        /// their execution.</remarks>
        /// <param name="packageKey">The unique key identifying the package to associate with the command. Cannot be null or empty.</param>
        /// <param name="command">The command delegate to register. Cannot be null.</param>
        /// <param name="modifiesEnvironment">Indicates whether the command modifies the environment. Set to <see langword="true"/> if the command alters
        /// environment variables or state; otherwise, <see langword="false"/>.</param>
        void AddCommand(string packageKey, ICommandDelegate command, bool modifiesEnvironment = false);
    }
}