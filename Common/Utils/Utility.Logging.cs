using MagicStorage.Common;
using MagicStorage.Common.Systems;
using ReLogic.Content;
using System;

namespace MagicStorage {
	partial class Utility {
		public static void WriteLineSafely(string text) {
			if (!AssetRepository.IsMainThread) {
				// Local capturing
				string t = text;

				ServerActionsQueue.QueueActionBasedOnClientPresence(() => WriteLineSafely_Inner(t));
			} else
				WriteLineSafely_Inner(text);
		}

		private static void WriteLineSafely_Inner(string text) {
			using (ConsoleColorLock.Acquire())
				Console.WriteLine(text);
		}

		public static void WriteLineColoredSafely(string text, ConsoleColor fg, ConsoleColor bg) {
			if (!AssetRepository.IsMainThread) {
				// Local capturing
				string t = text;
				ConsoleColor f = fg, b = bg;

				ServerActionsQueue.QueueActionBasedOnClientPresence(() => WriteLineColoredSafely_Inner(t, f, b));
			} else
				WriteLineColoredSafely_Inner(text, fg, bg);
		}

		private static void WriteLineColoredSafely_Inner(string text, ConsoleColor fg, ConsoleColor bg) {
			using (ConsoleColorLock.Acquire(fg, bg))
				Console.WriteLine(text);
		}

		public static void PrettyWriteLineToConsole(string text, ConsoleColor foregroundColor, ConsoleColor backgroundColor) => WriteLineColoredSafely(text, foregroundColor, backgroundColor);
	}
}
