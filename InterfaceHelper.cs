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
