using MagicStorage.CrossMod;
using Microsoft.Xna.Framework;
using SerousCommonLib.API;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;

namespace MagicStorage.Items.ErrorDisplay {
	[Autoload(false)]
	internal class BaseErrorDummyItem : ModItem {
		internal class Loadable : ILoadable {
			public void Load(Mod mod) {
				ModItem item;
				mod.AddContent(item = new BaseErrorDummyItem("Error_ItemSlotRenderFail"));
				RenderFailItemType = item.Type;

				mod.AddContent(item = new BaseErrorDummyItem("Error_ItemNetReadFail"));
				NetReadFailItemType = item.Type;

				mod.AddContent(item = new BaseErrorDummyItem("Error_ItemNBTFail"));
				NBTFailItemType = item.Type;

				// TODO: add "tooltips" mentioning extra information (e.g. the mod and name, if they could be read; what caused the error; etc.)
				// TODO: use actual tooltips instead of mouse text for better readability
			}

			public void Unload() {
				RenderFailItemType = 0;
				NetReadFailItemType = 0;
				NBTFailItemType = 0;
			}
		}

		internal const bool LoadTestItems = false;

		public static int RenderFailItemType { get; private set; }

		public static int NetReadFailItemType { get; private set; }

		public static int NBTFailItemType { get; private set; }

		[CloneByReference]
		public readonly string name;

		public string OriginalMod;
		public string OriginalName;
		public int OriginalPrefix;
		public TagCompound data;

		public override string Name => name;

		protected override bool CloneNewInstances => true;

		public override LocalizedText Tooltip => Language.GetText("Mods.MagicStorage.Items.ErrorItems.CommonTooltip");

		public BaseErrorDummyItem(string name) {
			this.name = name;
		}

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 0;
		}

		public override void SetDefaults() {
			Item.maxStack = int.MaxValue;
		}

		public override void SaveData(TagCompound tag) {
			tag["originalMod"] = OriginalMod;
			tag["originalName"] = OriginalName;
			tag["originalPrefix"] = OriginalPrefix;
			if (data is not null)
				tag["data"] = data;
		}

		public override void LoadData(TagCompound tag) {
			tag.TryGet("originalMod", out OriginalMod);
			tag.TryGet("originalName", out OriginalName);
			tag.TryGet("originalPrefix", out OriginalPrefix);
			tag.TryGet("data", out data);
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			int index = tooltips.FindIndex(t => t.Name == "ItemName");
			var replacementLine = new TooltipLine(Mod, "ErrorMessage", this.GetLocalizedValue("Message")) { OverrideColor = Color.Red };

			if (index >= 0)
				tooltips[index] = replacementLine;
			else
				tooltips.Insert(0, replacementLine);

			TooltipHelper.FindAndInsertLines(
				Mod,
				tooltips,
				"<FULLNAME>",
				static _ => "OriginalSource",
				string.IsNullOrWhiteSpace(OriginalMod) || string.IsNullOrWhiteSpace(OriginalName)
					? Language.GetTextValue("Mods.MagicStorage.Items.ErrorItems.NameTextUnknown")
					: Language.GetText("Mods.MagicStorage.Items.ErrorItems.NameText").Format(OriginalMod, OriginalName)
			);

			if (PrefixLoader.GetPrefix(OriginalPrefix) is ModPrefix modPrefix) {
				string modPrefixMod, modPrefixName;
				if (modPrefix is UnloadedPrefix) {
					var globalItem = Item.GetGlobalItem<UnloadedGlobalItem>();
					modPrefixMod = globalItem.ModPrefixMod;
					modPrefixName = globalItem.ModPrefixName;
				} else {
					modPrefixMod = modPrefix.Mod.Name;
					modPrefixName = modPrefix.Name;
				}

				TooltipHelper.FindAndInsertLines(
					Mod,
					tooltips,
					"<PREFIX>",
					static _ => "OriginalPrefix",
					Language.GetText("Mods.MagicStorage.Items.ErrorItems.PrefixTextModded").Format(modPrefixMod, modPrefixName)
				);
			} else if (OriginalPrefix != 0 && OriginalPrefix < PrefixID.Count) {
				TooltipHelper.FindAndInsertLines(
					Mod,
					tooltips,
					"<PREFIX>",
					static _ => "OriginalPrefix",
					Language.GetText("Mods.MagicStorage.Items.ErrorItems.PrefixText").Format(PrefixID.Search.GetName(OriginalPrefix))
				);
			} else
				TooltipHelper.FindAndRemoveLine(tooltips, "<PREFIX>");
		}

		internal class Aggregator : StorageAggregator {
			public override bool AppliesToItem(Item item) => item.ModItem is BaseErrorDummyItem;

			public override bool? CanAggregateItems(Item destination, Item checking) => false;
		}
	}
}
