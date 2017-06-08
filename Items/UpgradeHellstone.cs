using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class UpgradeHellstone : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Hellstone Storage Upgrade");
			Tooltip.SetDefault("Upgrades Storage Unit to 120 capacity"
				+ "\n<right> a Demonite/Crimtane Storage Unit to use");
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 99;
			item.rare = 2;
			item.value = Item.sellPrice(0, 0, 40, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.HellstoneBar, 10);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Topaz);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyTopaz");
			}
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}