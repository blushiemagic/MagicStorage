using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace MagicStorage.Items
{
	public class LocatorDisk : Locator
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Locator Drive");
			Tooltip.SetDefault("<right> Storage Heart to store location"
				+ "\n<right> Remote Storage Access to set it"
				+ "\nDoes not get destroyed upon use");
			DisplayName.AddTranslation(GameCulture.Russian, "Привод локатора");
			Tooltip.AddTranslation(GameCulture.Russian, "<right> по сердцу хранилища чтобы сохранить локацию"
			        + "\n<Right> по дистанционному доступу к хранилищу чтобы установить его"
				+ "\nНе разрушается при использовании");	
		}

		public override void SetDefaults()
		{
			item.width = 28;
			item.height = 28;
			item.maxStack = 1;
			item.rare = 10;
			item.value = Item.sellPrice(0, 5, 0, 0);
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.MartianConduitPlating, 25);
			recipe.AddIngredient(ItemID.LunarBar, 5);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
