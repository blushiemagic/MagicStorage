using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.IO {
	public class DeserializedNetItem {
		public int? type;
		public int prefix;
		public string modPrefixMod;
		public string modPrefixName;
		public int stack = 1;
		public bool favorite = false;

		public void GetContentNames(out string modName, out string contentName) {
			if (type is int readType) {
				if (readType < ItemID.Count) {
					// Vanilla item
					modName = "Terraria";
					contentName = ItemID.Search.GetName(readType);
					return;
				} else if (ModContent.GetModItem(readType) is ModItem modItem) {
					// Modded item
					modName = modItem.Mod.Name;
					contentName = modItem.Name;
					return;
				}
			}

			// ID didn't exist, or wasn't set
			modName = null;
			contentName = null;
		}

		public static DeserializedNetItem FromTagData(TagCompound tag) {
			DeserializedNetItem readData = new();
				
			if (tag.TryGet("id", out int netID)) {
				// Vanilla item
				readData.type = netID;
			} else if (tag.TryGet("mod", out string mod) && tag.TryGet("name", out string name) && ModContent.TryFind(mod, name, out ModItem modItem)) {
				// Modded item
				readData.type = modItem.Type;
			}

			if (tag.TryGet("prefix", out byte prefix)) {
				// Vanilla prefix
				readData.prefix = prefix;
			} else if (tag.TryGet("modPrefixMod", out string prefixMod) && tag.TryGet("modPrefixName", out string prefixName) && ModContent.TryFind(prefixMod, prefixName, out ModPrefix modPrefix)) {
				// Modded prefix
				readData.prefix = modPrefix.Type;
			}

			if (tag.TryGet("stack", out int stack))
				readData.stack = stack;

			if (tag.TryGet("fav", out bool favorite))
				readData.favorite = favorite;

			return readData;
		}

		public TagCompound ToTagData() {
			if (type is not int readType)
				return null;

			TagCompound tag = [];

			if (readType < ItemID.Count) {
				tag.Set("mod", "Terraria");
				tag.Set("id", readType);
			} else if (ModContent.GetModItem(readType) is ModItem modItem) {
				tag.Set("mod", modItem.Mod.Name);
				tag.Set("name", modItem.Name);
			} else
				return null;

			if (modPrefixMod is not null && modPrefixName is not null) {
				tag.Set("modPrefixMod", modPrefixMod);
				tag.Set("modPrefixName", modPrefixName);
			} else if (prefix != 0 && prefix < PrefixID.Count)
				tag.Set("prefix", (byte)prefix);

			if (stack > 1)
				tag.Set("stack", stack);

			if (favorite)
				tag.Set("fav", favorite);

			return tag;
		}
	}
}
