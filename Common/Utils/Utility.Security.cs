using MagicStorage.Common.Systems;
using System.IO;

namespace MagicStorage {
	partial class Utility {
		public static bool IsSuccess(this NetworkActionResult result) => result is NetworkActionResult.Success or NetworkActionResult.OperatorForcedSuccess;

		public static void WriteSecurityAccess(this BinaryWriter writer) {
			if (SecuritySystem.TryGetCurrentAccessContext(out var context)) {
				writer.Write(true);
				writer.Write((byte)context.Player);
			} else
				writer.Write(false);
		}

		public static bool ReadSecurityAccess(this BinaryReader reader, out SecuritySystem.AccessContext context, bool automaticallyUse = true) {
			if (reader.ReadBoolean()) {
				context = new(reader.ReadByte());

				if (automaticallyUse)
					context.Use();

				return true;
			} else {
				context = default;
				return false;
			}
		}
	}
}
