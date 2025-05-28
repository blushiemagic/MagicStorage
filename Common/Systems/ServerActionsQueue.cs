using System;
using System.Collections.Concurrent;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems {
	// Main.ConsumeAllMainThreadActions() doesn't run on servers unless clients are connected, for whatever reason
	internal class ServerActionsQueue : ModSystem {
		private static readonly ConcurrentQueue<Action> _actions = new();

		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				Main.OnTickForThirdPartySoftwareOnly += ConsumeActions;  // Fugly hack to allow actions to always get consumed on the server
		}

		public override void OnWorldUnload() {
			if (Main.netMode != NetmodeID.Server)
				return;

			// Ensure that all actions are consumed before unloading the world
			ConsumeActions();
		}

		public static void QueueActionBasedOnClientPresence(Action action) {
			if (Main.netMode != NetmodeID.Server)
				Main.QueueMainThreadAction(action);
			else
				QueueAction(action);
		}

		public static void QueueAction(Action action) {
			ArgumentNullException.ThrowIfNull(action);

			_actions.Enqueue(action);
		}

		private static void ConsumeActions() {
			while (_actions.TryDequeue(out var action))
				action();
		}
	}
}
