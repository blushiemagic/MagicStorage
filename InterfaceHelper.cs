using System;
using System.Collections.Generic;
using System.Reflection;
using MagicStorage.Components;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage
{
	public static class InterfaceHelper
	{
		private static FieldInfo _itemIconCacheTimeInfo;

		public static void Initialize()
		{
			_itemIconCacheTimeInfo = typeof(Main).GetField("_itemIconCacheTime", BindingFlags.NonPublic | BindingFlags.Static);

			if (_itemIconCacheTimeInfo is null)
				throw new Exception("Reflection value was null (source: InterfaceHelper.Initialize)");
		}

		public static void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int inventoryIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");
			if (inventoryIndex != -1)
				layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer("MagicStorage: StorageAccess", DrawStorageGUI, InterfaceScaleType.UI));
		}

		public static bool DrawStorageGUI()
		{
			Player player = Main.LocalPlayer;
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			Point16 storageAccess = modPlayer.ViewingStorage();
			if (Main.playerInventory && storageAccess.X >= 0 && storageAccess.Y >= 0)
			{
				ModTile modTile = TileLoader.GetTile(Main.tile[storageAccess.X, storageAccess.Y].TileType);
				if (modTile is StorageAccess access)
				{
					TEStorageHeart heart = access.GetHeart(storageAccess.X, storageAccess.Y);
					if (heart is not null)
					{
						if (access is EnvironmentAccess)
							EnvironmentGUI.Draw();
						else if (access is CraftingAccess)
							CraftingGUI.Draw();
						else
							StorageGUI.Draw();
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
			CalculatedStyle dimensions = element.GetDimensions();
			Vector2 vector = new(dimensions.X, dimensions.Y);
			Vector2 position = new Vector2(dimensions.Width, dimensions.Height) + vector;
			vector = Vector2.Transform(vector, Main.UIScaleMatrix);
			position = Vector2.Transform(position, Main.UIScaleMatrix);
			Rectangle result = new((int) vector.X, (int) vector.Y, (int) (position.X - vector.X), (int) (position.Y - vector.Y));
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
