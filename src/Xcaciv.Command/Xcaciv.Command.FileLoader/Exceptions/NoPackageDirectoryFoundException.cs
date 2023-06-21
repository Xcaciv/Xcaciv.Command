namespace Xcaciv.Command.FileLoader.Exceptions
{
    public class NoPackageDirectoryFoundException : Exception
    {
        public NoPackageDirectoryFoundException(string message) : base(message)
        {
        }

        public NoPackageDirectoryFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}