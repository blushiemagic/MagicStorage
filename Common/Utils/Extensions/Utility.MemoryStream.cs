using System.IO;

namespace MagicStorage {
	partial class Utility {
		public static void Reset(this MemoryStream @this) {
			@this.Position = 0;
			@this.SetLength(0);
			@this.Capacity = 0;  // Important!  This line is what ends up actually setting the internal buffer to an empty array.
		}
	}
}
