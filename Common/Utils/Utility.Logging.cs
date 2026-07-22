using MagicStorage.Common;
using MagicStorage.Common.Systems;
using ReLogic.Content;
using System;

namespace MagicStorage {
	partial class Utility {
		/// <summary>
		/// Writes a console line directly on the main thread or queues it safely from another thread.
		/// </summary>
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

		/// <summary>
		/// Writes a colored console line directly on the main thread or queues it safely from another thread.
		/// </summary>
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

		/// <summary>
		/// Writes a colored console line using the thread-safe console writer.
		/// </summary>
		public static void PrettyWriteLineToConsole(string text, ConsoleColor foregroundColor, ConsoleColor backgroundColor) => WriteLineColoredSafely(text, foregroundColor, backgroundColor);
	}
}
