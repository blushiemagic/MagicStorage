using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage.Common.IO {
	internal static class ReadWriteExtensions {
		public static void Write(this ValueWriter writer, Point16 position) {
			ModContent.GetInstance<WorldWidthTracker>().Send(position.X, writer);
			ModContent.GetInstance<WorldHeightTracker>().Send(position.Y, writer);
		}

		public static Point16 ReadPoint16(this ValueReader reader) {
			int x = 0, y = 0;
			ModContent.GetInstance<WorldWidthTracker>().Receive(ref x, reader);
			ModContent.GetInstance<WorldHeightTracker>().Receive(ref y, reader);
			return new Point16(x, y);
		}
	}
}
