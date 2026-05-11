using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Baldic.API.SaveSystem
{
    /// <summary>
    /// Read API for a single save chunk. Mirror of <see cref="ModSaveWriter"/>.
    /// </summary>
    public sealed class ModSaveReader : IDisposable
    {
        private readonly BinaryReader _reader;

        public ModSaveReader(Stream stream)
        {
            _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        }

        public bool   ReadBool()   => _reader.ReadBoolean();
        public int    ReadInt()    => _reader.ReadInt32();
        public long   ReadLong()   => _reader.ReadInt64();
        public float  ReadFloat()  => _reader.ReadSingle();
        public double ReadDouble() => _reader.ReadDouble();

        public string? ReadString()
        {
            bool hasValue = _reader.ReadBoolean();
            return hasValue ? _reader.ReadString() : null;
        }

        public byte[] ReadBytes()
        {
            int length = _reader.ReadInt32();
            return _reader.ReadBytes(length);
        }

        /// <summary>Deserialize a JSON-encoded object written by <see cref="ModSaveWriter.WriteJson{T}"/>.</summary>
        public T? ReadJson<T>()
        {
            string? json = ReadString();
            if (json == null) return default;
            return JsonConvert.DeserializeObject<T>(json);
        }

        public void Dispose() => _reader.Dispose();
    }
}
