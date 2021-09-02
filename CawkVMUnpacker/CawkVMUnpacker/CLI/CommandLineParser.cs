using System;
using System.IO;

namespace CawkVMUnpacker.CLI {
	internal sealed class CommandLineParser {
		private ParseError errorValue;
		private string errorArg = string.Empty;

		internal bool Parse(string[] args, out CommandLineArguments parsedArgs) {
			parsedArgs = new CommandLineArguments();

			if (args.Length < 1) {
				errorValue = ParseError.BadArgCount;
				return false;
			}

			if (args[0] == "-h" || args[0] == "--help") {
				parsedArgs.ShowHelp = true;
				return true;
			}

			// Check for invalid characters in path.
			try {
				parsedArgs.FilePath = Path.GetFullPath(args[0]);
			}
			catch {
				errorValue = ParseError.InvalidFilePath;
				return false;
			}

			if (!File.Exists(parsedArgs.FilePath)) {
				errorValue = ParseError.InvalidFilePath;
				return false;
			}

			for (int i = 1; i < args.Length; i++) {
				switch (args[i]) {
					case "--preserveMD":
					case "-p":
						parsedArgs.PreserveMetadata = true;
						break;
					case "--dataName":
					case "-d":
						parsedArgs.DataResourceName = args[++i];
						break;
					default:
						errorValue = ParseError.InavlidArgument;
						errorArg = args[i];
						return false;
				}
			}

			return true;
		}

		internal string GetError() {
			switch (errorValue) {
				case ParseError.BadArgCount:
					return "Too little arguments.";
				case ParseError.InavlidArgument:
					return $"Inavlid argument '{errorArg}'";
				case ParseError.InavlidArgValue:
					return $"Inavlid argument value '{errorArg}'";
				case ParseError.InvalidFilePath:
					return "Provided file does not exist!";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private enum ParseError {
			BadArgCount,
			InvalidFilePath,
			InavlidArgument,
			InavlidArgValue
		}
	}
}
