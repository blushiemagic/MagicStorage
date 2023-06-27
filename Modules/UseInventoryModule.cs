using MagicStorage.Common.Systems;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace MagicStorage.Modules {
	internal class UseInventoryModule : EnvironmentModule {
		private int[] types = new int[58];
		private int[] stacks = new int[58];

		public override IEnumerable<Item> GetAdditionalItems(EnvironmentSandbox sandbox) => sandbox.player.inventory.Take(58);

		public override void PreUpdateUI() {
			Item[] inv = Main.LocalPlayer.inventory;

			bool needRefresh = false;

			for (int i = 0; i < 58; i++) {
				Item item = inv[i];

				if (types[i] != item.type) {
					types[i] = item.type;
					needRefresh = true;
				}

				if (stacks[i] != item.stack) {
					stacks[i] = item.stack;
					needRefresh = true;
				}
			}

			if (needRefresh && MagicUI.IsCraftingUIOpen())
				StorageGUI.needRefresh = true;
		}
	}
}
