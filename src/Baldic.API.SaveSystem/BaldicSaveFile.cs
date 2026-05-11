using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Baldic.Loader.Abstractions;

namespace Baldic.API.SaveSystem
{
    /// <summary>
    /// Low-level read/write for the Baldic modded save file format.
    ///
    /// File layout:
    /// <code>
    /// [4 bytes] magic "BLDC"
    /// [1 byte]  format version (1)
    /// [string]  gameVersion
    /// [string]  loaderVersion
    /// [int]     active mod count
    ///   [string]  modId (repeated)
    /// [int]     chunk count
    ///   [chunk] (repeated):
    ///     [string] chunkId (namespace:name)
    ///     [int]    schemaVersion
    ///     [int]    data length
    ///     [bytes]  data
    /// </code>
    ///
    /// Vanilla save files are never read or written by this class.
    /// </summary>
    public sealed class BaldicSaveFile
    {
        public const string MagicHeader = "BLDC";
        public const byte FormatVersion = 1;

        public string GameVersion { get; set; } = "0.0.0";
        public string LoaderVersion { get; set; } = "0.1.0";
        public List<string> ActiveMods { get; set; } = new List<string>();

        /// <summary>Raw chunk data, keyed by namespaced id string.</summary>
        public Dictionary<string, SaveChunk> Chunks { get; } =
            new Dictionary<string, SaveChunk>(StringComparer.Ordinal);

        public void AddChunk(NamespacedId id, int schemaVersion, byte[] data)
        {
            Chunks[id.ToString()] = new SaveChunk(id, schemaVersion, data);
        }

        public bool TryGetChunk(NamespacedId id, out SaveChunk chunk)
        {
            return Chunks.TryGetValue(id.ToString(), out chunk);
        }

        // ── Serialization ───────────────────────────────────────────────────

        public void WriteTo(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            w.Write(Encoding.ASCII.GetBytes(MagicHeader));
            w.Write(FormatVersion);
            w.Write(GameVersion);
            w.Write(LoaderVersion);
            w.Write(ActiveMods.Count);
            foreach (var mod in ActiveMods) w.Write(mod);
            w.Write(Chunks.Count);
            foreach (var kvp in Chunks)
            {
                var chunk = kvp.Value;
                w.Write(chunk.ChunkId.ToString());
                w.Write(chunk.SchemaVersion);
                w.Write(chunk.Data.Length);
                w.Write(chunk.Data);
            }
        }

        public static BaldicSaveFile ReadFrom(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            byte[] magic = r.ReadBytes(4);
            if (Encoding.ASCII.GetString(magic) != MagicHeader)
                throw new InvalidDataException("Not a Baldic save file (bad magic).");

            byte fmtVer = r.ReadByte();
            if (fmtVer != FormatVersion)
                throw new InvalidDataException($"Unsupported Baldic save format version {fmtVer}.");

            var save = new BaldicSaveFile
            {
                GameVersion = r.ReadString(),
                LoaderVersion = r.ReadString(),
            };

            int modCount = r.ReadInt32();
            for (int i = 0; i < modCount; i++)
                save.ActiveMods.Add(r.ReadString());

            int chunkCount = r.ReadInt32();
            for (int i = 0; i < chunkCount; i++)
            {
                string idStr = r.ReadString();
                int schemaVer = r.ReadInt32();
                int dataLen = r.ReadInt32();
                byte[] data = r.ReadBytes(dataLen);

                if (NamespacedId.TryParse(idStr, out var id))
                    save.Chunks[idStr] = new SaveChunk(id, schemaVer, data);
            }

            return save;
        }

        public static bool IsBaldicSaveFile(string path)
        {
            try
            {
                using var fs = File.OpenRead(path);
                byte[] hdr = new byte[4];
                return fs.Read(hdr, 0, 4) == 4 && Encoding.ASCII.GetString(hdr) == MagicHeader;
            }
            catch { return false; }
        }
    }

    public sealed class SaveChunk
    {
        public NamespacedId ChunkId { get; }
        public int SchemaVersion { get; }
        public byte[] Data { get; }

        public SaveChunk(NamespacedId chunkId, int schemaVersion, byte[] data)
        {
            ChunkId = chunkId;
            SchemaVersion = schemaVersion;
            Data = data;
        }
    }
}
