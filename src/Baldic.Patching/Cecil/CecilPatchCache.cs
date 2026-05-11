using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Baldic.Patching.Cecil
{
    /// <summary>
    /// Manages a content-addressed cache for patched assemblies.
    /// Cache key = SHA256(original_asm) + SHA256(all_patcher_versions_concatenated).
    /// This ensures the cache is invalidated when the game updates OR when any patcher changes.
    /// </summary>
    public sealed class CecilPatchCache
    {
        private readonly string _cacheRoot;

        public CecilPatchCache(string cacheRoot)
        {
            _cacheRoot = cacheRoot;
        }

        /// <summary>
        /// Returns the cached patched assembly path if valid, null otherwise.
        /// </summary>
        public string? TryGetCached(string originalPath, string cacheKey)
        {
            string cachedPath = GetCachePath(originalPath, cacheKey);
            return File.Exists(cachedPath) ? cachedPath : null;
        }

        /// <summary>
        /// Write the patched assembly bytes to the cache.
        /// </summary>
        public string Write(string originalPath, string cacheKey, byte[] patchedBytes)
        {
            string cachedPath = GetCachePath(originalPath, cacheKey);
            Directory.CreateDirectory(Path.GetDirectoryName(cachedPath)!);
            File.WriteAllBytes(cachedPath, patchedBytes);
            return cachedPath;
        }

        /// <summary>
        /// Compute a cache key from the original assembly hash and a combined patcher descriptor.
        /// </summary>
        public static string ComputeCacheKey(string originalPath, string patcherDescriptor)
        {
            using var sha = SHA256.Create();
            byte[] originalHash = sha.ComputeHash(File.ReadAllBytes(originalPath));
            byte[] descHash = sha.ComputeHash(Encoding.UTF8.GetBytes(patcherDescriptor));

            var combined = new byte[originalHash.Length + descHash.Length];
            Buffer.BlockCopy(originalHash, 0, combined, 0, originalHash.Length);
            Buffer.BlockCopy(descHash, 0, combined, originalHash.Length, descHash.Length);

            byte[] finalHash = sha.ComputeHash(combined);
            var sb = new StringBuilder(finalHash.Length * 2);
            foreach (byte b in finalHash) sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }

        /// <summary>Remove all cached files that don't have a corresponding current key.</summary>
        public void CleanStale()
        {
            if (!Directory.Exists(_cacheRoot)) return;
            foreach (var file in Directory.EnumerateFiles(_cacheRoot, "*.dll", SearchOption.AllDirectories))
            {
                try { File.Delete(file); }
                catch { /* ignore locked files */ }
            }
        }

        /// <summary>
        /// Copy the content-addressed cached file to the flat <c>cache/managed/</c> directory
        /// which is configured as <c>dll_search_path_override</c> in doorstop config.
        /// Mono will find the patched assembly here before the original Managed/ folder.
        /// </summary>
        public void PublishToManagedCache(string originalPath, string cachedPath)
        {
            string managedCacheDir = Path.Combine(_cacheRoot, "managed");
            Directory.CreateDirectory(managedCacheDir);
            string dest = Path.Combine(managedCacheDir, Path.GetFileName(originalPath));
            File.Copy(cachedPath, dest, overwrite: true);
        }

        private string GetCachePath(string originalPath, string cacheKey)
        {
            string asmName = Path.GetFileName(originalPath);
            return Path.Combine(_cacheRoot, "cecil", cacheKey.Substring(0, 16), asmName);
        }
    }
}
