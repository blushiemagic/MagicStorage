using SerousCommonLib.API;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public class EnvironmentAccess : ModItem {
		public override void SetStaticDefaults() {
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
		}

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = 99;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(gold: 1, silver: 35);
			Item.createTile = ModContent.TileType<Components.EnvironmentAccess>();
		}

		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<StorageComponent>();
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 2);
			recipe.AddIngredient(ItemID.DirtBlock, 50);
			recipe.AddIngredient(ItemID.StoneBlock, 50);
			recipe.AddIngredient(ItemID.MudBlock, 50);
			recipe.AddIngredient(ItemID.SnowBlock, 50);
			recipe.AddIngredient(ItemID.SandBlock, 50);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}

		private const int MaxCounter = 20;
		private static int tooltipCounter = MaxCounter;
		private static string lastKnownTooltip;

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			IEnumerable<EnvironmentModule> available = EnvironmentModuleLoader.modules.Where(e => e.IsAvailable());

			if (!available.Any()) {
				TooltipHelper.FindAndModify(tooltips, "<MODULES>", "  None");
				return;
			}

			if (++tooltipCounter < MaxCounter) {
				TooltipHelper.FindAndInsertLines(Mod, tooltips, "<MODULES>", i => "ModuleName_" + i, lastKnownTooltip);
				return;
			}

			tooltipCounter = 0;

			List<string> lines = available.Select(GetModuleDisplayName).ToList();

			const int maxLines = 7;
			
			static IEnumerable<int> GenerateIndices(int count, int max) {
				HashSet<int> indices = new();

				if (count <= max)
					return Enumerable.Range(0, count);

				for (int i = 0; i < max; i++)
					while(!indices.Add(Main.rand.Next(count)));

				return indices;
			}

			IEnumerable<string> random = GenerateIndices(lines.Count, maxLines).OrderBy(i => i).Select(i => lines[i]);

			TooltipHelper.FindAndInsertLines(Mod, tooltips, "<MODULES>", i => "ModuleName_" + i, lastKnownTooltip = "  " + string.Join("\n  ", random));
		}

		private static string GetModuleDisplayName(EnvironmentModule e) {
			return e.DisplayName.GetTranslation(Language.ActiveCulture);
		}
	}
}
