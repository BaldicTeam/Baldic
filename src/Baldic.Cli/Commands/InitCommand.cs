using System;
using System.IO;
using System.Text;

namespace Baldic.Cli.Commands
{
    /// <summary>
    /// <c>baldic init &lt;modid&gt; [--dir &lt;path&gt;] [--name &lt;name&gt;] [--author &lt;author&gt;]</c>
    ///
    /// Scaffolds a new Baldic mod project directory.
    /// </summary>
    public static class InitCommand
    {
        public static int Run(string modId, string? outputDir, string? modName, string? authorName)
        {
            // Validate modId
            if (string.IsNullOrWhiteSpace(modId) || !System.Text.RegularExpressions.Regex.IsMatch(modId, @"^[a-z][a-z0-9_]{1,63}$"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Invalid mod id '{modId}'. Must match ^[a-z][a-z0-9_]{{1,63}}$");
                Console.ResetColor();
                return 1;
            }

            modName ??= ToPascalCase(modId).Replace("_", " ");
            authorName ??= Environment.UserName;

            string targetDir = outputDir ?? Path.Combine(Directory.GetCurrentDirectory(), modId);
            string className = ToPascalCase(modId);

            if (Directory.Exists(targetDir))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Directory '{targetDir}' already exists. Files will be created if missing.");
                Console.ResetColor();
            }

            Directory.CreateDirectory(targetDir);
            Directory.CreateDirectory(Path.Combine(targetDir, "assets", "localization", "English"));

            WriteIfMissing(Path.Combine(targetDir, $"{className}.csproj"),
                GenerateCsproj(className));

            WriteIfMissing(Path.Combine(targetDir, "baldic.mod.json"),
                GenerateManifest(modId, modName, authorName));

            WriteIfMissing(Path.Combine(targetDir, $"{className}Initializer.cs"),
                GenerateInitializer(className, modId));

            WriteIfMissing(Path.Combine(targetDir, "assets", "localization", "English", "main.json"),
                "{\n}\n");

            WriteIfMissing(Path.Combine(targetDir, ".gitignore"),
                "bin/\nobj/\n*.baldicmod\nBaldic.GameReferences.props\n*.user\n");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Created mod project: {targetDir}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine($"  cd {targetDir}");
            Console.WriteLine($"  baldic game set <path-to-game>");
            Console.WriteLine($"  dotnet build");
            Console.WriteLine($"  baldic install");

            return 0;
        }

        private static void WriteIfMissing(string path, string content)
        {
            if (File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, Encoding.UTF8);
            Console.WriteLine($"  created: {Path.GetFileName(path)}");
        }

        private static string ToPascalCase(string id)
        {
            var sb = new StringBuilder();
            bool upper = true;
            foreach (char c in id)
            {
                if (c == '_') { upper = true; continue; }
                sb.Append(upper ? char.ToUpper(c) : c);
                upper = false;
            }
            return sb.ToString();
        }

        private static string GenerateCsproj(string className) => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Baldic.API.Core""    Version=""0.1.0"" />
    <PackageReference Include=""Baldic.SDK.MSBuild"" Version=""0.1.0"" PrivateAssets=""all"" />
  </ItemGroup>

  <ItemGroup>
    <BaldicModManifest Include=""baldic.mod.json"" />
    <BaldicModAsset    Include=""assets\**\*"" />
  </ItemGroup>
</Project>
";

        private static string GenerateManifest(string modId, string modName, string author) => $@"{{
  ""schemaVersion"": 1,
  ""id"": ""{modId}"",
  ""version"": ""1.0.0"",
  ""name"": ""{modName}"",
  ""description"": ""A Baldic mod for Baldi's Basics Plus."",
  ""authors"": [
    {{ ""name"": ""{author}"" }}
  ],
  ""license"": ""MIT"",
  ""environment"": ""client"",
  ""game"": {{
    ""id"": ""baldis-basics-plus"",
    ""versions"": ["">=0.14.0 <0.15.0""]
  }},
  ""loader"": {{
    ""versions"": ["">=0.1.0""]
  }},
  ""depends"": {{
    ""baldic"": "">=0.1.0"",
    ""baldic-api"": "">=0.1.0""
  }},
  ""assemblies"": [""lib/{modId}.dll""],
  ""entrypoints"": {{
    ""main"": [""{ToPascalCase(modId)}.{ToPascalCase(modId)}Initializer""]
  }}
}}
";

        private static string GenerateInitializer(string className, string modId) => $@"using System;
using Baldic.Loader.Abstractions.Entrypoints;

namespace {className}
{{
    public sealed class {className}Initializer : IBaldicModInitializer
    {{
        public void OnInitialize(ModInitializationContext context)
        {{
            Console.WriteLine($""[{modId}] Initialized!"");
        }}
    }}
}}
";

    }
}
