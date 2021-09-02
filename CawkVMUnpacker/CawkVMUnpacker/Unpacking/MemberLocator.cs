using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace CawkVMUnpacker.Unpacking {
	internal sealed class MemberLocator {
		private readonly ModuleDef _module;
		private readonly string? _proposedResourceName;

		internal EmbeddedResource? DataResource { get; private set; }
		internal IMethod? RunnerMethod { get; private set; }

		internal MemberLocator(ModuleDef module, string? proposedResourceName) {
			_module = module;
			_proposedResourceName = proposedResourceName;
		}

		[MemberNotNullWhen(true, "DataResource", "RunnerMethod")]
		internal bool LocateRequiredMembers() {
			EmbeddedResource? dataResource = null;

			if (!string2.IsNullOrEmpty(_proposedResourceName))
				dataResource = FindResourceInModule(_proposedResourceName);

			if (dataResource is null) {
				string? resourceName = FindCawkVMDataResourceName(_module.EntryPoint) ??
									   FindCawkVMDataResourceName(_module.GlobalType.FindStaticConstructor());

				dataResource = FindResourceInModule(resourceName);
			}

			if (dataResource is null)
				return false;

			var runnerMethod = LocateRunnerMethod();
			if (runnerMethod is null)
				return false;

			DataResource = dataResource;
			RunnerMethod = runnerMethod;
			return true;
		}

		private EmbeddedResource? FindResourceInModule(string? resourceName) {
			return resourceName is null ? null : _module.Resources.FindEmbeddedResource(resourceName);
		}

		private static string? FindCawkVMDataResourceName(MethodDef methodDef) {
			if (!methodDef.HasBody)
				return null;

			int? index = methodDef.Body.FindFirstCodeIndex(Code.Call);
			if (!index.HasValue)
				return null;

			int? ldStrIndex = methodDef.Body.FindFirstNonNopPrecedingCode(index.Value);
			if (!ldStrIndex.HasValue)
				return null;

			var ldstrInstr = methodDef.Body.Instructions[ldStrIndex.Value];
			if (ldstrInstr.OpCode.Code != Code.Ldstr || ldstrInstr.Operand is not string str)
				return null;

			return str;
		}

		private IMethod? LocateRunnerMethod() {
			Dictionary<IMethod, int> calls =
				new Dictionary<IMethod, int>(MethodEqualityComparer.CompareDeclaringTypes);

			int callCount = 0;
			foreach (var type in _module.GetTypes()) {
				if (callCount >= 40)
					break;
				foreach (var method in type.Methods) {
					if (method.IsInstanceConstructor || method.IsStaticConstructor || !method.HasBody)
						continue;

					foreach (var instruction in method.Body.Instructions) {
						if (instruction.OpCode.Code != Code.Call || instruction.Operand is not IMethod calledMethod)
							continue;
						if (calledMethod.NumberOfGenericParameters != 0)
							continue;
						if (!DnlibUtils.IsMethod(calledMethod, "System.Object", "(System.Int32,System.Int32,System.Int32,System.Object[])"))
							continue;

						calls.TryGetValue(calledMethod, out int calledCount);
						calls[calledMethod] = calledCount + 1;
						callCount++;
					}
				}
			}

			IMethod? mostCalled = null;
			callCount = 0;
			foreach (var key in calls.Keys) {
				if (calls[key] > callCount) {
					callCount = calls[key];
					mostCalled = key;
				}
			}

			return mostCalled;
		}
	}
}
