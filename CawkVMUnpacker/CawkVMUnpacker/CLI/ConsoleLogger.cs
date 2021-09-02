using System;

namespace CawkVMUnpacker.CLI {
	internal sealed class ConsoleLogger : ILogger {
		public void Info(string str) {
			CLIUtils.WriteInColor("[", ConsoleColor.Gray);
			CLIUtils.WriteInColor("INFO", ConsoleColor.White);
			CLIUtils.WriteLineInColor($"] {str}", ConsoleColor.Gray);
		}

		public void Debug(string str) {
			CLIUtils.WriteLineInColor($"[DEBUG] {str}", ConsoleColor.Gray);
		}

		public void Warn(string str) {
			CLIUtils.WriteInColor("[", ConsoleColor.Gray);
			CLIUtils.WriteInColor("WARN", ConsoleColor.Yellow);
			CLIUtils.WriteLineInColor($"] {str}", ConsoleColor.Gray);
		}

		public void Error(string str) {
			CLIUtils.WriteInColor("[", ConsoleColor.Gray);
			CLIUtils.WriteInColor("ERROR", ConsoleColor.Red);
			CLIUtils.WriteLineInColor($"] {str}", ConsoleColor.Gray);
		}
	}
}
