using System.Collections.Generic;

namespace MagicStorage {
	/// <summary>
	/// Registry and lookup helpers for loaded <see cref="EnvironmentModule"/> instances.
	/// </summary>
	public static class EnvironmentModuleLoader {
		internal static List<EnvironmentModule> modules = new();
		/// <summary>
		/// The number of registered environment modules.
		/// </summary>
		public static int Count { get; private set; }

		internal static int Add(EnvironmentModule module) {
			modules.Add(module);
			Count++;
			return Count - 1;
		}

		/// <summary>
		/// Gets the environment module registered for <paramref name="index"/>, or <see langword="null"/> when the index is invalid.
		/// </summary>
		public static EnvironmentModule Get(int index) => index < 0 || index >= modules.Count ? null : modules[index];

		internal static void Unload() {
			modules.Clear();
			Count = 0;
		}
	}
}
