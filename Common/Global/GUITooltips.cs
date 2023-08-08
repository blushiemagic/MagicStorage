using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
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

		private static readonly FieldInfo UnloadedGlobalItem_data = typeof(UnloadedGlobalItem).GetField("data", BindingFlags.NonPublic | BindingFlags.Instance);

		internal static IEnumerable<(string, string)> Sources(Item item) {
			var globalsEnumerator = item.Globals.GetEnumerator();
			var globals = new List<GlobalItem>();

			// "ref struct" enumerators can't be used in "yield return" blocks...
			while (globalsEnumerator.MoveNext())
				globals.Add(globalsEnumerator.Current);
			
			foreach (GlobalItem gItem in globals) {
				TagCompound tag = new();

				// Account for UnloadedGlobalItem no longer using SaveData in 1.4.4
				if (gItem is UnloadedGlobalItem unloaded) {
					var data = UnloadedGlobalItem_data.GetValue(unloaded) as IList<TagCompound>;
					if (data.Count > 0)
						tag["modData"] = data;
				} else
					gItem.SaveData(item, tag);

				if (tag.Count > 0) {
					yield return (gItem.Mod.Name + "/" + gItem.Name, ToBase64(tag));
				}
			}
		}


		private static string ToBase64(TagCompound tag) {
			MemoryStream ms = new();
			TagIO.ToStream(tag, ms, true);
			return Convert.ToBase64String(ms.ToArray());
		}
	}
}
