namespace Xcaciv.Command.FileLoader.Exceptions
{
    public class NoPluginsFoundException : Exception
    {
        public NoPluginsFoundException(string message) : base(message)
        {
        }

        public NoPluginsFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}