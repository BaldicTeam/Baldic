using System;
using System.IO;
using Baldic.Loader.Abstractions;

namespace Baldic.Loader.Logging
{
    /// <summary>
    /// Thread-safe file logger that writes to a single log file.
    /// Format: [yyyy-MM-dd HH:mm:ss.fff] [Level] [source] message
    /// </summary>
    public sealed class FileLogger : IBaldicLog, IDisposable
    {
        private readonly string _source;
        private readonly StreamWriter? _writer;
        private readonly object _lock = new object();
        private bool _disposed;

        public static readonly IBaldicLog Null = new NullLogger();

        public FileLogger(string source, string filePath)
        {
            _source = source;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                _writer = new StreamWriter(filePath, append: false) { AutoFlush = true };
            }
            catch
            {
                _writer = null;
            }
        }

        public void Trace(string message) => Write("Trace", message);
        public void Debug(string message) => Write("Debug", message);
        public void Info(string message)  => Write("Info ", message);
        public void Warn(string message)  => Write("Warn ", message);
        public void Error(string message) => Write("Error", message);
        public void Fatal(string message) => Write("Fatal", message);

        private void Write(string level, string message)
        {
            if (_writer == null) return;
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{_source}] {message}";
            lock (_lock)
            {
                if (!_disposed)
                {
                    _writer.WriteLine(line);
                    Console.WriteLine(line);
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _writer?.Dispose();
                }
            }
        }

        private sealed class NullLogger : IBaldicLog
        {
            public void Trace(string message) { }
            public void Debug(string message) { }
            public void Info(string message)  { }
            public void Warn(string message)  { }
            public void Error(string message) { }
            public void Fatal(string message) { }
        }
    }
}
