using System;
using System.Collections.Generic;
using System.IO;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Abstractions.Manifest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Baldic.Loader.Manifest
{
    /// <summary>
    /// Parses <c>baldic.mod.json</c> content into a <see cref="ModManifest"/>.
    /// Does NOT validate the manifest — call <see cref="ManifestValidator"/> separately.
    /// </summary>
    public static class ManifestParser
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>Parse manifest from a JSON string.</summary>
        public static ManifestParseResult ParseJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return ManifestParseResult.Fail("BALDIC_M001", "Manifest JSON is empty.");

            ModManifest manifest;
            try
            {
                var obj = JObject.Parse(json);
                manifest = obj.ToObject<ModManifest>(JsonSerializer.Create(Settings))!;
                if (manifest == null)
                    return ManifestParseResult.Fail("BALDIC_M002", "Failed to deserialize manifest.");
            }
            catch (JsonException ex)
            {
                return ManifestParseResult.Fail("BALDIC_M003", $"JSON parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ManifestParseResult.Fail("BALDIC_M004", $"Unexpected parse error: {ex.Message}");
            }

            return ManifestValidator.Validate(manifest);
        }

        /// <summary>Parse manifest from a file path.</summary>
        public static ManifestParseResult ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
                return ManifestParseResult.Fail("BALDIC_M010", $"Manifest file not found: '{filePath}'");

            string json;
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                return ManifestParseResult.Fail("BALDIC_M011", $"Cannot read manifest file '{filePath}': {ex.Message}");
            }

            return ParseJson(json);
        }
    }
}
