using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage
{
	public class BlockRecipes : GlobalRecipe
	{
		public static bool active = true;
		public static object activeLock = new object();

		public override bool RecipeAvailable(Recipe recipe)
		{
			if (!active)
				return true;
			try
			{
				Player player = Main.LocalPlayer;
				var modPlayer = player.GetModPlayer<StoragePlayer>();
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
