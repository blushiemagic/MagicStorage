#nullable enable
using MagicStorage.Items;
using SerousCommonLib.API;
using Terraria;
using Terraria.ID;
using Terraria.Recipe;

namespace MagicStorage.Edits;

public class BiomeGlobeDetour : Edit
{
	public override void LoadEdits()
	{
		OnRecipe.FindRecipes += Recipe_FindRecipes;
	}

	public override void UnloadEdits()
	{
		OnRecipe.FindRecipes -= Recipe_FindRecipes;
	}

	private static void Recipe_FindRecipes(OnRecipe.orig_FindRecipes orig, bool canDelayCheck)
	{
		// For whatever reason, this hook can end up running during worldgen and the main menu
		if (Main.gameMenu || WorldGen.gen) {
			orig(canDelayCheck);
			return;
		}

		Player player = Main.LocalPlayer;

		bool oldGraveyard = player.ZoneGraveyard;
		bool oldSnow = player.ZoneSnow;
		bool oldNearCampfire = player.adjTile[TileID.Campfire];
		bool oldAltar = player.adjTile[TileID.DemonAltar];
		bool oldWater = player.adjWater;
		bool oldLava = player.adjLava;
		bool oldHoney = player.adjHoney;

		//Override these flags
		if (player.GetModPlayer<BiomePlayer>().biomeGlobe)
		{
			player.ZoneGraveyard = true;
			player.ZoneSnow = true;
			player.adjTile[TileID.Campfire] = true;
			player.adjTile[TileID.DemonAltar] = true;
			player.adjWater = true;
			player.adjLava = true;
			player.adjHoney = true;
		}

		orig(canDelayCheck);

		player.ZoneGraveyard = oldGraveyard;
		player.ZoneSnow = oldSnow;
		player.adjTile[TileID.Campfire] = oldNearCampfire;
		player.adjTile[TileID.DemonAltar] = oldAltar;
		player.adjWater = oldWater;
		player.adjLava = oldLava;
		player.adjHoney = oldHoney;
	}
}
