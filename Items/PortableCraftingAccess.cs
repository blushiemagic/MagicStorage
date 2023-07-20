using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items {
	public class PortableCraftingAccess : PortableAccess {
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Purple;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAnimation = 28;
			Item.useTime = 28;
			Item.value = Item.sellPrice(gold: 10);
		}

		protected override void OpenContext(out int validTileType, out string missingAccessKey, out string unlocatedAccessKey, out bool openCrafting) {
			validTileType = ModContent.TileType<Components.CraftingAccess>();
			missingAccessKey = "Mods.MagicStorage.PortableCraftingMissing";
			unlocatedAccessKey = "Mods.MagicStorage.PortableCraftingUnlocated";
			openCrafting = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PortableCraftingAccessHM>()
				.AddIngredient(ItemID.MartianConduitPlating, 20)
				.AddIngredient(ItemID.LunarBar, 15)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
