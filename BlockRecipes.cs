using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage
{
	public class BlockRecipes : GlobalRecipe
	{
		public override bool RecipeAvailable(Recipe recipe)
		{
			try
			{
				Player player = Main.player[Main.myPlayer];
				StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>(mod);
				Point16 storageAccess = modPlayer.ViewingStorage();
				return storageAccess.X < 0 || storageAccess.Y < 0;
			}
			catch
			{
				return true;
			}
		}
	}
}