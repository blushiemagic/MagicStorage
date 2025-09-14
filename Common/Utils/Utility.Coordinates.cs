using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;

namespace MagicStorage {
	partial class Utility {
		public static void ConvertToGPSCoordinates(Vector2 worldCoordinate, out int compassCoordinate, out int depthCoordinate) {
			// Copy/paste of logic from the info accessories
			compassCoordinate = (int)(worldCoordinate.X * 2f / 16f - Main.maxTilesX);
			depthCoordinate = (int)(worldCoordinate.Y * 2f / 16f - Main.worldSurface * 2.0);
		}

		public static void ConvertToGPSCoordinates(Point16 tileCoordinate, out int compassCoordinate, out int depthCoordinate) {
			// Copy/paste of logic from the info accessories
			compassCoordinate = (int)(tileCoordinate.X * 2f - Main.maxTilesX);
			depthCoordinate = (int)(tileCoordinate.Y * 2f - Main.worldSurface * 2.0);
		}

		public static void ConvertToGPSCoordinates(Vector2 worldCoordinate, out string compassText, out string depthText) {
			// Copy/paste of logic from the info accessories
			ConvertToGPSCoordinates(worldCoordinate, out int compass, out int depth);

			GetGPSText(compass, depth, out compassText, out depthText);
		}

		public static void GetGPSText(int compass, int depth, out string compassText, out string depthText) {
			// Reverse the depth conversion
			float worldCoordinateY = (float)(depth + Main.worldSurface * 2.0f) / 2f * 16f;

			// Get the compass text
			if (compass > 0)
				compassText = Language.GetTextValue("GameUI.CompassEast", compass);
			else if (compass == 0)
				compassText = Language.GetTextValue("GameUI.CompassCenter");
			else
				compassText = Language.GetTextValue("GameUI.CompassWest", -compass);

			// Get the depth text
			float sizeFactor = Main.maxTilesX / 4200f;
			sizeFactor *= sizeFactor;

			int cavernsOffset = 1200;

			float surface = (float)((worldCoordinateY / 16f - (65f + 10f * sizeFactor)) / (Main.worldSurface / 5.0));

			string layerText;
			if (worldCoordinateY > (Main.maxTilesY - 204) * 16)
				layerText = Language.GetTextValue("GameUI.LayerUnderworld");
			else if (worldCoordinateY > Main.rockLayer * 16.0 + cavernsOffset / 2f + 16.0)
				layerText = Language.GetTextValue("GameUI.LayerCaverns");
			else if (depth > 0)
				layerText = Language.GetTextValue("GameUI.LayerUnderground");
			else if (surface < 1f)
				layerText = Language.GetTextValue("GameUI.LayerSpace");
			else
				layerText = Language.GetTextValue("GameUI.LayerSurface");

			depth = Math.Abs(depth);

			string coordText = depth == 0 ? Language.GetTextValue("GameUI.DepthLevel") : Language.GetTextValue("GameUI.Depth", depth);

			depthText = $"{coordText} {layerText}";
		}
	}
}
