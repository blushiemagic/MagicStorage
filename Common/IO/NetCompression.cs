using System.Collections.Generic;
using Terraria.ModLoader;

namespace MagicStorage.Common.IO {
	/// <summary>
	/// This helper class contains methods for maximizing compression of data for I/O
	/// </summary>
	public static partial class NetCompression {
		private class Loadable : ILoadable {
			void ILoadable.Load(Mod mod) { }

			void ILoadable.Unload() => Unload();
		}

		private static readonly List<IDataSizeTracker> _trackers = new();

		internal static int Add(IDataSizeTracker tracker) {
			_trackers.Add(tracker);
			return _trackers.Count - 1;
		}

		private static void Unload() {
			_trackers.Clear();
		}
	}
}
