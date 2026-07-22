using MagicStorage.Common.Systems;
using System.IO;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Checks whether a network action result represents a successful operation.
		/// </summary>
		/// <param name="result">The result to inspect.</param>
		/// <returns><see langword="true" /> if the result is successful; otherwise, <see langword="false" />.</returns>
		public static bool IsSuccess(this NetworkActionResult result) => result is NetworkActionResult.Success or NetworkActionResult.OperatorForcedSuccess;

		/// <summary>
		/// Writes the current security access context to a binary stream.
		/// </summary>
		/// <param name="writer">The writer to write to.</param>
		public static void WriteSecurityAccess(this BinaryWriter writer) {
			if (SecuritySystem.TryGetCurrentAccessContext(out var context)) {
				writer.Write(true);
				writer.Write((byte)context.Player);
			} else
				writer.Write(false);
		}

		/// <summary>
		/// Reads a security access context from a binary stream.
		/// </summary>
		/// <param name="reader">The reader to read from.</param>
		/// <param name="context">The access context that was read.</param>
		/// <param name="automaticallyUse">Whether to immediately activate the context when present.</param>
		/// <returns><see langword="true" /> if a context was present; otherwise, <see langword="false" />.</returns>
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
