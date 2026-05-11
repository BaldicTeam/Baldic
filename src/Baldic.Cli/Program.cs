using System;
using Baldic.Cli.Commands;

namespace Baldic.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return 0;
            }

            string command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "doctor":
                    return RunDoctor(args);

                case "init":
                    return RunInit(args);

                case "install":
                    return RunInstall(args);

                case "install-loader":
                    return RunInstallLoader(args, uninstall: false);

                case "uninstall-loader":
                    return RunInstallLoader(args, uninstall: true);

                case "version":
                    Console.WriteLine("Baldic CLI 0.1.0");
                    return 0;

                case "help":
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Unknown command: '{command}'");
                    Console.ResetColor();
                    PrintHelp();
                    return 1;
            }
        }

        private static int RunDoctor(string[] args)
        {
            string? gamePath = null;
            string? knownVersion = null;
            string? saveProfile = null;

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--game":
                    case "-g":
                        gamePath = args.Length > i + 1 ? args[++i] : null;
                        break;
                    case "--version":
                    case "-v":
                        knownVersion = args.Length > i + 1 ? args[++i] : null;
                        break;
                    case "--save-profile":
                    case "-s":
                        saveProfile = args.Length > i + 1 ? args[++i] : null;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(gamePath))
            {
                // Try common Steam path for BB+.
                string steamDefault = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "share", "Steam", "steamapps", "common", "Baldi's Basics Plus");

                if (System.IO.Directory.Exists(steamDefault))
                {
                    gamePath = steamDefault;
                    Console.WriteLine($"Auto-detected game at: {gamePath}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Usage: baldic doctor --game <path-to-game>");
                    Console.ResetColor();
                    return 1;
                }
            }

            return DoctorCommand.Run(gamePath, knownVersion, saveProfile);
        }

        private static int RunInit(string[] args)
        {
            string? modId = args.Length > 1 ? args[1] : null;
            string? dir = null, name = null, author = null;
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--dir" && i + 1 < args.Length) dir = args[++i];
                else if (args[i] == "--name" && i + 1 < args.Length) name = args[++i];
                else if (args[i] == "--author" && i + 1 < args.Length) author = args[++i];
            }
            if (string.IsNullOrWhiteSpace(modId))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Usage: baldic init <modid> [--dir <path>] [--name <name>] [--author <name>]");
                Console.ResetColor();
                return 1;
            }
            return InitCommand.Run(modId, dir, name, author);
        }

        private static int RunInstall(string[] args)
        {
            string? gamePath = null, modPath = null;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--game" && i + 1 < args.Length) gamePath = args[++i];
                else if (args[i] == "--mod"  && i + 1 < args.Length) modPath  = args[++i];
            }
            return InstallCommand.Run(gamePath, modPath);
        }

        private static int RunInstallLoader(string[] args, bool uninstall)
        {
            string? gamePath = null;
            for (int i = 1; i < args.Length; i++)
                if (args[i] == "--game" && i + 1 < args.Length) gamePath = args[++i];
            return uninstall
                ? InstallLoaderCommand.RunUninstall(gamePath)
                : InstallLoaderCommand.RunInstall(gamePath);
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Baldic CLI 0.1.0");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  doctor           [--game <p>] [--version <v>] [--save-profile <out>]");
            Console.WriteLine("  init <modid>     [--dir <p>] [--name <n>] [--author <a>]");
            Console.WriteLine("  install          [--game <p>] [--mod <path.baldicmod>]");
            Console.WriteLine("  install-loader   [--game <p>]");
            Console.WriteLine("  uninstall-loader [--game <p>]");
            Console.WriteLine("  version");
            Console.WriteLine("  help");
        }
    }
}
