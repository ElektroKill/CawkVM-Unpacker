using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.IO;

namespace CawkVMUnpacker.Unpacking {
	internal sealed class Unpacker {
		private readonly ILogger logger;
		private readonly IUnpackerParameters parameters;

		internal Unpacker(ILogger logger, IUnpackerParameters parameters) {
			this.logger = logger;
			this.parameters = parameters;
		}

		internal bool Run() {
			try {
				var asmResolver = new AssemblyResolver();
				asmResolver.DefaultModuleContext = new ModuleContext(asmResolver);

				logger.Info("Loading module...");

				var module = ModuleDefMD.Load(parameters.FilePath, asmResolver.DefaultModuleContext);
				module.Location = parameters.FilePath;

				logger.Info("Locating required module members...");

				var locator = new MemberLocator(module, parameters.DataResourceName);
				if (!locator.LocateRequiredMembers()) {
					logger.Error("Failed to locate required members for unpacking!");
					return false;
				}

				logger.Debug($"Found data resource with name '{locator.DataResource.Name}'.");
				logger.Debug($"Found runner method reference with token 0x{locator.RunnerMethod.MDToken}.");

				logger.Info("Locating protected methods...");
				var protectedMethods = FindProtectedMethods(module, locator.RunnerMethod);
				logger.Debug($"Found {protectedMethods.Count} protected methods!");

				logger.Info("Decrypting protected method data...");
				var decrypted = DecryptProtectedMethodData(module, locator.DataResource, protectedMethods);
				logger.Debug("Succcessfully decrypted method data.");

				logger.Info("Converting protected methods to CIL...");
				var converter = new CawkVMToCilBodyConverter(module);
				foreach (var decryptedInfo in decrypted) {
					var (instructions, exceptionHandlers) = CawkVMMethodDataReader.ReadVMMethodData(decryptedInfo.Value);

					decryptedInfo.Key.Body = converter.Convert(decryptedInfo.Key, instructions, exceptionHandlers);
				}

				logger.Info("Writing module...");
				string newFilePath = Path.ChangeExtension(parameters.FilePath, "unpacked" + Path.GetExtension(parameters.FilePath));
				WriteModule(module, newFilePath);

				logger.Info($"Successfully restored {decrypted.Count} methods!");

				module.Dispose();
				return true;
			}
			catch (Exception ex) {
				logger.Error($"An error occured during unpacking: {ex}");
				return false;
			}
		}

		private Dictionary<MethodDef, RawMethodData> FindProtectedMethods(ModuleDef module, IMethod runnerMethod) {
			var result = new Dictionary<MethodDef, RawMethodData>();

			foreach (var type in module.GetTypes()) {
				foreach (var method in type.Methods) {
					if (!method.HasBody)
						continue;

					var instrs = method.Body.Instructions;
					int callIndex = -1;
					for (int i = 0; i < instrs.Count; i++) {
						var instr = instrs[i];
						if (instr.OpCode.Code == Code.Call && instr.Operand == runnerMethod) {
							callIndex = i;
							break;
						}
					}

					if (callIndex == -1)
						continue;

					int? ldlocIndex = method.Body.FindFirstNonNopPrecedingCode(callIndex);
					if (!ldlocIndex.HasValue || !instrs[ldlocIndex.Value].IsLdloc()) {
						logger.Error($"Failed to find ldloc instruction in method 0x{method.MDToken}.");
						continue;
					}

					int? idIndex = method.Body.FindFirstNonNopPrecedingCode(ldlocIndex.Value);
					if (!idIndex.HasValue || !instrs[idIndex.Value].IsLdcI4()) {
						logger.Error($"Failed to find id value in method 0x{method.MDToken}.");
						continue;
					}

					int? sizeIndex = method.Body.FindFirstNonNopPrecedingCode(idIndex.Value);
					if (!sizeIndex.HasValue || !instrs[sizeIndex.Value].IsLdcI4()) {
						logger.Error($"Failed to find size value in method 0x{method.MDToken}.");
						continue;
					}

					int? offsetIndex = method.Body.FindFirstNonNopPrecedingCode(sizeIndex.Value);
					if (!offsetIndex.HasValue || !instrs[offsetIndex.Value].IsLdcI4()) {
						logger.Error($"Failed to find offset value in method 0x{method.MDToken}.");
						continue;
					}

					var methodData = new RawMethodData {
						Length = instrs[sizeIndex.Value].GetLdcI4Value(),
						Offset = instrs[offsetIndex.Value].GetLdcI4Value()
					};

					result[method] = methodData;
				}
			}

			return result;
		}

		private static Dictionary<MethodDef, byte[]> DecryptProtectedMethodData(ModuleDefMD module, EmbeddedResource dataResource, IDictionary<MethodDef, RawMethodData> rawInfo) {
			Dictionary<MethodDef, byte[]> result = new Dictionary<MethodDef, byte[]>();

			using MD5 md5 = MD5.Create();

			byte[] decryptedResourceData = CawkVMEncryption.SeededRandomKeyXOR(dataResource.CreateReader().ReadRemainingBytes());
			var reader = ByteArrayDataReaderFactory.CreateReader(decryptedResourceData);

			foreach (var rawMethodData in rawInfo) {
				reader.Position = (uint)rawMethodData.Value.Offset;
				byte[] encryptedData = reader.ReadBytes(rawMethodData.Value.Length);

				byte[] nativeKey = rawMethodData.Key.GetOriginalRawILBytes(module);
				byte[] xored = CawkVMEncryption.DecryptNative(encryptedData, nativeKey);

				byte[] rijndaelKey = md5.ComputeHash(Encoding.ASCII.GetBytes(rawMethodData.Key.Name));
				byte[] fullyDecrypted = CawkVMEncryption.DecryptRijndael(xored, rijndaelKey);
				result[rawMethodData.Key] = fullyDecrypted;
			}

			return result;
		}

		private void WriteModule(ModuleDefMD module, string filePath) {
			ModuleWriterOptionsBase modOpts;

			if (!module.IsILOnly || module.VTableFixups != null) {
				modOpts = new NativeModuleWriterOptions(module, true) {
					KeepWin32Resources = true,
					KeepExtraPEData = true
				};
			}
			else
				modOpts = new ModuleWriterOptions(module);

			if (parameters.PreserveMetadata) {
				modOpts.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				modOpts.MetadataOptions.PreserveHeapOrder(module, true);
			}

			modOpts.Logger = new DnlibLogger(logger);
			modOpts.MetadataLogger = new DnlibLogger(logger);

			if (modOpts is NativeModuleWriterOptions nativeOptions)
				module.NativeWrite(filePath, nativeOptions);
			else
				module.Write(filePath, (ModuleWriterOptions)modOpts);
		}
	}
}
