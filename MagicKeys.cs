using MagicStorage.Common.Global;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace MagicStorage {
	internal class MagicKeys : ModPlayer {
		public static ModKeybind PrintBase64Data;

		public override void Load() {
			if (Main.dedServ)
				return;

			PrintBase64Data = KeybindLoader.RegisterKeybind(Mod, "Print Item Data", Keys.I);
		}

		public override void ProcessTriggers(TriggersSet triggersSet) {
			if (!MagicStorageConfig.AllowItemDataDebug)
				return;

			if (Main.HoverItem?.IsAir ?? true)
				return;

			if (!GUITooltips.CanAddTooltips())
				return;

			if (PrintBase64Data.JustPressed) {
				Item item = Main.HoverItem;

				var sources = GUITooltips.Sources(item);

				if (!sources.Any())
					return;

				Main.NewTextMultiline($"Data for item \"{Lang.GetItemNameValue(item.type)}\":\n" + string.Join('\n', sources.Select(t => t.Item1 + "\n  " + t.Item2)), c: Color.Orange);
			}
		}
	}
}
