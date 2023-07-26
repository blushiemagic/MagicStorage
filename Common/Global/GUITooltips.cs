using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.Global {
	internal class GUITooltips : GlobalItem {
		public static bool CanAddTooltips() {
			if (Main.gameMenu)
				return false;

			if (StoragePlayer.LocalPlayer.StorageCrafting() || StoragePlayer.LocalPlayer.StorageEnvironment() || StoragePlayer.LocalPlayer.ViewingStorage().X < 0)
				return false;

			return !MagicUI.blockItemSlotActionsDetour;
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			if (!MagicStorageConfig.ItemDataDebug)
				return;

			if (!CanAddTooltips())
				return;

			//The cursor is actually within the GUI.  Add the tooltip to the item
			string[] sources = Sources(item).Select(t => t.Item1).ToArray();

			var binds = MagicKeys.PrintBase64Data.GetAssignedKeys();
			string keybindingKey = "\"" + (binds.Count == 0 ? "<NOT BOUND>" : binds[0]) + "\"";

			string whole;
			string keyInfo = "Press " + keybindingKey + " to print this item's encoded data to the chat.";

			if (sources.Length > 0) {
				whole = "This item contains GlobalItem data.\n" +
					"Sources:\n" +
					"  " + string.Join("\n  ", sources.Distinct()) + "\n" +
					keyInfo;
			} else
				whole = keyInfo;

			int index = 0;
			foreach (string line in whole.Split('\n')) {
				tooltips.Insert(index, new(Mod, "GlobalItemDebug_" + index, $"[c/{Color.Yellow.Hex3()}:{line}]"));
				index++;
			}
		}

		internal static IEnumerable<(string, string)> Sources(Item item) {
			foreach (var instanced in item.Globals) {
				GlobalItem gItem = instanced.Instance;

				TagCompound tag = new();
				gItem.SaveData(item, tag);

				if (tag.Count > 0)
					yield return (gItem.Mod.Name + "/" + gItem.Name, ToBase64(tag));
			}
		}

		internal static string ToBase64(TagCompound tag) {
			MemoryStream ms = new();
			TagIO.ToStream(tag, ms, true);
			return Convert.ToBase64String(ms.ToArray());
		}
	}
}
