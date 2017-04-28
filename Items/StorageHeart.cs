using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Items
{
	public class StorageHeart : ModItem
	{
		public override void SetDefaults()
		{
			item.name = "Storage Heart";
			item.width = 26;
			item.height = 26;
			item.maxStack = 99;
			item.useTurn = true;
			item.autoReuse = true;
			item.useAnimation = 15;
			item.useTime = 10;
			item.useStyle = 1;
			item.consumable = true;
			item.rare = 1;
			item.value = Item.sellPrice(0, 1, 35, 0);
			item.createTile = mod.TileType("StorageHeart");
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(null, "StorageComponent");
			recipe.AddRecipeGroup("MagicStorage:AnyDiamond", 5);
			if (MagicStorage.legendMod == null)
			{
				recipe.AddIngredient(ItemID.Emerald, 10);
			}
			else
			{
				recipe.AddRecipeGroup("MagicStorage:AnyEmerald", 10);
			}
			recipe.AddTile(TileID.WorkBenches);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}