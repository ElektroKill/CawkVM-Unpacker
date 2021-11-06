using System;
using System.Reflection;
using CawkVMUnpacker.Unpacking;

namespace CawkVMUnpacker.CLI {
	internal sealed class Program {
		private static int Main(string[] args) {
			int retValue = 1;

			var assembly = typeof(Program).Assembly;
			var productAttr = (AssemblyProductAttribute)assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
			var verAttr = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0];
			string fullProductName = $"{productAttr.Product} {verAttr.InformationalVersion}";

			CLIUtils.WriteLineInColor(fullProductName, ConsoleColor.White);
			CLIUtils.WriteLineInColor("~ Now with 100% less reflection ~", ConsoleColor.White);
			Console.WriteLine();

			string origTitle = Console.Title;
			Console.Title = $"{fullProductName} - Running...";

			var parser = new CommandLineParser();
			if (parser.Parse(args, out var parsedArgs)) {
				if (parsedArgs.ShowHelp) {
					Console.Title = $"{fullProductName} - Help";
					CLIUtils.WriteLineInColor($"Usage: {AppDomain.CurrentDomain.FriendlyName} {{FilePath}} {{Options}}{Environment.NewLine}", ConsoleColor.Gray);
					CLIUtils.WriteLineInColor("Options:", ConsoleColor.Gray);
					CLIUtils.WriteLineInColor("    --help|-h                     Showns this screen.", ConsoleColor.Gray);
					CLIUtils.WriteLineInColor("    --preserveMD|-p               Preserves all metadata when writing.", ConsoleColor.Gray);
					CLIUtils.WriteLineInColor("    --keepPE|-k                   Preserves all Win32 resources and extra PE data when writing.", ConsoleColor.Gray);
					CLIUtils.WriteLineInColor("    --dataName|-d                 Specify CawkVM data resource name.", ConsoleColor.Gray);
					retValue = 0;
				}
				else {
					var unp = new Unpacker(new ConsoleLogger(), parsedArgs);
					bool success = unp.Run();

					if (success) {
						Console.Title = $"{fullProductName} - Success";
						retValue = 0;
					}
					else
						Console.Title = $"{fullProductName} - Fail";
				}
			}
			else {
				Console.Title = $"{fullProductName} - Error";
				CLIUtils.WriteLineInColor($"Argument Error: {parser.GetError()}", ConsoleColor.Red);
				CLIUtils.WriteLineInColor("Use -h or --help.", ConsoleColor.Yellow);
			}

			Console.ResetColor();
			Console.ReadKey(true);
			Console.Title = origTitle;
			return retValue;
		}
	}
}
