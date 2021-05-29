using System.Collections.Generic;
using System.Reflection;
using MagicStorageExtra.Components;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorageExtra
{
	public static class InterfaceHelper
	{
		private static FieldInfo _itemIconCacheTimeInfo;

		public static void Initialize()
		{
			_itemIconCacheTimeInfo = typeof(Main).GetField("_itemIconCacheTime", BindingFlags.NonPublic | BindingFlags.Static);
		}

		public static void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			if (!Main.instance.IsActive) return;
			for (int k = 0; k < layers.Count; k++)
				if (layers[k].Name == "Vanilla: Inventory")
				{
					layers.Insert(k + 1, new LegacyGameInterfaceLayer("MagicStorageExtra: StorageAccess", DrawStorageGUI, InterfaceScaleType.UI));
					k++;
				}
		}

		public static bool DrawStorageGUI()
		{
			Player player = Main.LocalPlayer;
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			Point16 storageAccess = modPlayer.ViewingStorage();
			if (Main.playerInventory && storageAccess.X >= 0 && storageAccess.Y >= 0)
			{
				ModTile modTile = TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].type);
				if (modTile is StorageAccess access)
				{
					TEStorageHeart heart = access.GetHeart(storageAccess.X, storageAccess.Y);
					if (heart != null)
					{
						if (access is CraftingAccess)
							CraftingGUI.Draw(heart);
						else
							StorageGUI.Draw(heart);
					}
				}
			}

			return true;
		}

		public static void HideItemIconCache()
		{
			_itemIconCacheTimeInfo.SetValue(null, 0);
		}

		public static Rectangle GetFullRectangle(UIElement element)
		{
			var vector = new Vector2(element.GetDimensions().X, element.GetDimensions().Y);
			Vector2 position = new Vector2(element.GetDimensions().Width, element.GetDimensions().Height) + vector;
			vector = Vector2.Transform(vector, Main.UIScaleMatrix);
			position = Vector2.Transform(position, Main.UIScaleMatrix);
			var result = new Rectangle((int) vector.X, (int) vector.Y, (int) (position.X - vector.X), (int) (position.Y - vector.Y));
			int width = Main.spriteBatch.GraphicsDevice.Viewport.Width;
			int height = Main.spriteBatch.GraphicsDevice.Viewport.Height;
			result.X = Utils.Clamp(result.X, 0, width);
			result.Y = Utils.Clamp(result.Y, 0, height);
			result.Width = Utils.Clamp(result.Width, 0, width - result.X);
			result.Height = Utils.Clamp(result.Height, 0, height - result.Y);
			return result;
		}
	}
}