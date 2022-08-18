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
			if (!MagicStorageConfig.ItemDataDebug)
				return;

			if (Main.HoverItem?.IsAir ?? true)
				return;

			if (!GUITooltips.CanAddTooltips())
				return;

			if (PrintBase64Data.JustPressed) {
				Item item = Main.HoverItem;

				var sources = GUITooltips.Sources(item);

				string sourceText = sources.Any()
					? string.Join('\n', sources.Select(t => t.Item1 + "\n  " + t.Item2))
					: "None";

				Main.NewTextMultiline($"TagCompound data for item \"{Lang.GetItemNameValue(item.type)}\":\n"
					+ sourceText,
					c: Color.Orange);
			}
		}
	}
}
