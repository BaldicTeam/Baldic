namespace Baldic.Loader.Abstractions
{
    /// <summary>
    /// Minimal logging interface used throughout the loader.
    /// Implementations must be thread-safe.
    /// </summary>
    public interface IBaldicLog
    {
        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Fatal(string message);
    }
}
