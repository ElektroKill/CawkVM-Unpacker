namespace CawkVMUnpacker {
	internal interface ILogger {
		void Info(string str);
		void Debug(string str);
		void Warn(string str);
		void Error(string str);
	}
}
