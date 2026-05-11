using System;
using System.IO;
using System.Reflection;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Resources.Assets
{
    /// <summary>
    /// Raw file asset loader. Port of MTM101BMDE <c>AssetLoader</c>.
    ///
    /// All methods use reflection to call Unity APIs, avoiding a compile-time
    /// dependency on UnityEngine assemblies. This lets the project build
    /// without the game installed; at runtime the actual Unity types are present.
    ///
    /// For production content, prefer <see cref="ModAssetBundle"/> (AssetBundles).
    /// These helpers are kept for simple mods and dev mode.
    /// </summary>
    public static class AssetLoader
    {
        // Lazily resolved Unity types.
        private static Type? _texture2DType;
        private static Type? _imageConversionType;
        private static Type? _audioClipType;
        private static Type? _wwwType;

        private static Type Texture2DType => _texture2DType ??= RequireType(
            "UnityEngine.Texture2D, UnityEngine.CoreModule",
            "UnityEngine.Texture2D, UnityEngine");

        private static Type ImageConversionType => _imageConversionType ??= RequireType(
            "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule",
            "UnityEngine.ImageConversion, UnityEngine");

        private static Type AudioClipType => _audioClipType ??= RequireType(
            "UnityEngine.AudioClip, UnityEngine.AudioModule",
            "UnityEngine.AudioClip, UnityEngine");

        // ───────────────────────────────────────────── Texture ──

        /// <summary>
        /// Load a Texture2D from a PNG or JPG file on disk.
        /// Returns an <c>object</c> to avoid Unity compile-time dependency.
        /// Cast to UnityEngine.Texture2D at the call site.
        /// </summary>
        public static object? TextureFromFile(string path, IBaldicLog log)
        {
            if (!File.Exists(path)) { log.Warn($"[AssetLoader] Texture file not found: {path}"); return null; }
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                return TextureFromBytes(bytes, log);
            }
            catch (Exception ex) { log.Error($"[AssetLoader] TextureFromFile failed for '{path}': {ex.Message}"); return null; }
        }

        /// <summary>Decode raw PNG/JPG bytes into a Texture2D.</summary>
        public static object? TextureFromBytes(byte[] bytes, IBaldicLog log)
        {
            try
            {
                // new Texture2D(2, 2)
                var texture = Activator.CreateInstance(Texture2DType, 2, 2);

                // ImageConversion.LoadImage(texture, bytes)
                var loadImage = ImageConversionType.GetMethod("LoadImage",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { Texture2DType, typeof(byte[]) }, null);

                loadImage?.Invoke(null, new[] { texture, bytes });
                return texture;
            }
            catch (Exception ex) { log.Error($"[AssetLoader] TextureFromBytes failed: {ex.Message}"); return null; }
        }

        /// <summary>
        /// Create a Sprite from a Texture2D.
        /// pixelsPerUnit defaults to 100.
        /// Returns an <c>object</c> (UnityEngine.Sprite).
        /// </summary>
        public static object? SpriteFromTexture(object texture, float pixelsPerUnit, IBaldicLog log)
        {
            try
            {
                var spriteType = RequireType(
                    "UnityEngine.Sprite, UnityEngine.CoreModule",
                    "UnityEngine.Sprite, UnityEngine");
                var rect2Type = RequireType(
                    "UnityEngine.Rect, UnityEngine.CoreModule",
                    "UnityEngine.Rect, UnityEngine");
                var vec2Type  = RequireType(
                    "UnityEngine.Vector2, UnityEngine.CoreModule",
                    "UnityEngine.Vector2, UnityEngine");

                // Rect(0, 0, width, height)
                int width  = (int)Texture2DType.GetProperty("width")!.GetValue(texture)!;
                int height = (int)Texture2DType.GetProperty("height")!.GetValue(texture)!;
                object rect = Activator.CreateInstance(rect2Type, 0f, 0f, (float)width, (float)height)!;
                object pivot = Activator.CreateInstance(vec2Type, 0.5f, 0.5f)!;

                // Sprite.Create(texture, rect, pivot, pixelsPerUnit)
                var create = spriteType.GetMethod("Create",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { Texture2DType, rect2Type, vec2Type, typeof(float) }, null);

                return create?.Invoke(null, new[] { texture, rect, pivot, pixelsPerUnit });
            }
            catch (Exception ex) { log.Error($"[AssetLoader] SpriteFromTexture failed: {ex.Message}"); return null; }
        }

        // ───────────────────────────────────────────── Audio ──

        /// <summary>
        /// Load an AudioClip from a WAV file synchronously.
        /// Supports PCM 8-bit, 16-bit and 32-bit, any channel count and sample rate.
        /// Returns an <c>object</c> (UnityEngine.AudioClip). OGG/MP3 are not supported
        /// by this method — use UnityWebRequestMultimedia in a coroutine for those.
        /// </summary>
        public static object? AudioClipFromFile(string path, IBaldicLog log)
        {
            if (!File.Exists(path)) { log.Warn($"[AssetLoader] Audio file not found: {path}"); return null; }
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".wav")
            {
                log.Warn($"[AssetLoader] AudioClipFromFile: '{ext}' not supported synchronously. Use .wav or a coroutine.");
                return null;
            }
            try
            {
                byte[] raw = File.ReadAllBytes(path);
                return WavToAudioClip(Path.GetFileNameWithoutExtension(path), raw, log);
            }
            catch (Exception ex) { log.Error($"[AssetLoader] AudioClipFromFile failed for '{path}': {ex.Message}"); return null; }
        }

        private static object? WavToAudioClip(string clipName, byte[] data, IBaldicLog log)
        {
            if (data.Length < 44) throw new InvalidOperationException("WAV data too short.");
            if (System.Text.Encoding.ASCII.GetString(data, 0, 4) != "RIFF")
                throw new InvalidOperationException("Not a RIFF file.");
            if (System.Text.Encoding.ASCII.GetString(data, 8, 4) != "WAVE")
                throw new InvalidOperationException("RIFF type is not WAVE.");

            int channels = 0, sampleRate = 0, bitsPerSample = 0, dataOffset = 0, dataSize = 0;
            int pos = 12;

            while (pos + 8 <= data.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(data, pos, 4);
                int size = BitConverter.ToInt32(data, pos + 4);

                if (id == "fmt ")
                {
                    int fmt = BitConverter.ToInt16(data, pos + 8);
                    if (fmt != 1) throw new InvalidOperationException($"WAV format {fmt} not supported (PCM=1 required).");
                    channels      = BitConverter.ToInt16(data, pos + 10);
                    sampleRate    = BitConverter.ToInt32(data, pos + 12);
                    bitsPerSample = BitConverter.ToInt16(data, pos + 22);
                }
                else if (id == "data")
                {
                    dataOffset = pos + 8;
                    dataSize   = size;
                    break;
                }
                pos += 8 + size;
            }

            if (dataOffset == 0)  throw new InvalidOperationException("No data chunk in WAV.");
            if (channels == 0)    throw new InvalidOperationException("No fmt chunk in WAV.");

            int bytesPerSample = bitsPerSample / 8;
            int totalSamples   = dataSize / bytesPerSample;
            int sampleCount    = totalSamples / channels;

            float[] samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                int off = dataOffset + i * bytesPerSample;
                samples[i] = bitsPerSample switch
                {
                    8  => (data[off] - 128) / 128.0f,
                    16 => BitConverter.ToInt16(data, off) / 32768.0f,
                    32 => BitConverter.ToInt32(data, off) / 2147483648.0f,
                    _  => throw new InvalidOperationException($"Unsupported bit depth {bitsPerSample}.")
                };
            }

            // AudioClip.Create(name, lengthSamples, channels, frequency, stream)
            var create = AudioClipType.GetMethod("Create",
                BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(bool) }, null)
                ?? throw new MissingMethodException("AudioClip.Create not found.");

            object clip = create.Invoke(null, new object[] { clipName, sampleCount, channels, sampleRate, false })
                ?? throw new InvalidOperationException("AudioClip.Create returned null.");

            // clip.SetData(samples, 0)
            var setData = AudioClipType.GetMethod("SetData",
                BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(float[]), typeof(int) }, null)
                ?? throw new MissingMethodException("AudioClip.SetData not found.");

            setData.Invoke(clip, new object[] { samples, 0 });
            return clip;
        }

        // ───────────────────────────────────────────── Helpers ──

        private static Type RequireType(params string[] typeNames)
        {
            foreach (var name in typeNames)
            {
                var t = Type.GetType(name);
                if (t != null) return t;
            }
            throw new TypeLoadException($"None of the Unity types could be resolved: {string.Join(", ", typeNames)}. " +
                "Make sure UnityEngine assemblies are present in the AppDomain.");
        }
    }
}
