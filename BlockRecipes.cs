using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MagicStorage
{
	public class BlockRecipes : GlobalRecipe
	{
		public static bool Active = true;
		public static readonly object ActiveLock = new();

		public override bool RecipeAvailable(Recipe recipe)
		{
			if (!Active)
				return true;
			try
			{
				Player player = Main.LocalPlayer;
				StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
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
