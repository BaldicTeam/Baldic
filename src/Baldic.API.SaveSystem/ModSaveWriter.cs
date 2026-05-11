using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Baldic.API.SaveSystem
{
    /// <summary>
    /// Write API for a single save chunk. Wraps a <see cref="BinaryWriter"/> with
    /// convenience methods for primitives and JSON-serializable objects.
    /// </summary>
    public sealed class ModSaveWriter : IDisposable
    {
        private readonly BinaryWriter _writer;

        public ModSaveWriter(Stream stream)
        {
            _writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        }

        public void WriteBool(bool value)    => _writer.Write(value);
        public void WriteInt(int value)      => _writer.Write(value);
        public void WriteLong(long value)    => _writer.Write(value);
        public void WriteFloat(float value)  => _writer.Write(value);
        public void WriteDouble(double value)=> _writer.Write(value);

        public void WriteString(string? value)
        {
            _writer.Write(value != null);
            if (value != null) _writer.Write(value);
        }

        public void WriteBytes(byte[] data)
        {
            _writer.Write(data.Length);
            _writer.Write(data);
        }

        /// <summary>
        /// Serialize an object as JSON and write it as a length-prefixed UTF-8 string.
        /// Safe for complex nested structures. Not suitable for very large objects.
        /// </summary>
        public void WriteJson<T>(T value)
        {
            string json = JsonConvert.SerializeObject(value);
            WriteString(json);
        }

        public void Dispose() => _writer.Dispose();
    }
}
