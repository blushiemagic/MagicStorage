using System;
using System.Runtime.InteropServices;

namespace MagicStorage.Common {
	/// <summary>
	/// A structure used to temporarily overwrite the value in a boolean variable.  This type is intended to be used in <see langword="using"/> contexts
	/// </summary>
	public readonly ref struct FlagSwitch {
		private readonly Span<bool> _flag;
		private readonly bool _old;

		public FlagSwitch(ref bool flag, bool value) {
			_old = flag;
			_flag = MemoryMarshal.CreateSpan(ref flag, 1);
			flag = value;
		}

		public void Dispose() {
			_flag[0] = _old;
		}

		public static FlagSwitch Create(ref bool flag, bool value) => new FlagSwitch(ref flag, value);
	}
}
