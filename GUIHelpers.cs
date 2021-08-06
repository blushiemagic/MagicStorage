using System;
using System.Collections.Generic;
using MagicStorage.UI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage
{
	public class GUIHelpers
	{
		public static UIButtonChoice MakeSortButtons(Action onChanged)
		{
			return new UIButtonChoice(onChanged, new[]
			{
				TextureAssets.InventorySort[0],
				ModContent.Request<Texture2D>("Assets/SortID"),
				ModContent.Request<Texture2D>("Assets/SortName"),
				ModContent.Request<Texture2D>("Assets/SortNumber"),
				ModContent.Request<Texture2D>("Assets/SortNumber")
			}, new[]
			{
				Language.GetText("Mods.MagicStorage.SortDefault"),
				Language.GetText("Mods.MagicStorage.SortID"),
				Language.GetText("Mods.MagicStorage.SortName"),
				Language.GetText("Mods.MagicStorage.SortValue"),
				Language.GetText("Mods.MagicStorage.SortDps")
			});
		}

		public static UIButtonChoice MakeFilterButtons(bool withHistory, Action onChanged)
		{
			List<Asset<Texture2D>> textures = new()
			{
				ModContent.Request<Texture2D>("Assets/FilterAll"),
				ModContent.Request<Texture2D>("Assets/FilterMelee"),
				ModContent.Request<Texture2D>("Assets/FilterRanged"),
				ModContent.Request<Texture2D>("Assets/FilterMagic"),
				ModContent.Request<Texture2D>("Assets/FilterSummon"),
				ModContent.Request<Texture2D>("Assets/FilterThrowing"),
				ModContent.Request<Texture2D>("Assets/FilterAmmo"),
				ModContent.Request<Texture2D>("Assets/FilterPickaxe"),
				ModContent.Request<Texture2D>("Assets/FilterArmor"),
				ModContent.Request<Texture2D>("Assets/FilterEquips"),
				ModContent.Request<Texture2D>("Assets/FilterVanity"),
				ModContent.Request<Texture2D>("Assets/FilterPotion"),
				ModContent.Request<Texture2D>("Assets/FilterTile"),
				ModContent.Request<Texture2D>("Assets/FilterMisc")
			};
			List<LocalizedText> texts = new()
			{
				Language.GetText("Mods.MagicStorage.FilterAll"),
				Language.GetText("Mods.MagicStorage.FilterWeaponsMelee"),
				Language.GetText("Mods.MagicStorage.FilterWeaponsRanged"),
				Language.GetText("Mods.MagicStorage.FilterWeaponsMagic"),
				Language.GetText("Mods.MagicStorage.FilterWeaponsSummon"),
				Language.GetText("Mods.MagicStorage.FilterWeaponsThrown"),
				Language.GetText("Mods.MagicStorage.FilterAmmo"),
				Language.GetText("Mods.MagicStorage.FilterTools"),
				Language.GetText("Mods.MagicStorage.FilterArmor"),
				Language.GetText("Mods.MagicStorage.FilterEquips"),
				Language.GetText("Mods.MagicStorage.FilterVanity"),
				Language.GetText("Mods.MagicStorage.FilterPotions"),
				Language.GetText("Mods.MagicStorage.FilterTiles"),
				Language.GetText("Mods.MagicStorage.FilterMisc")
			};
			if (withHistory)
			{
				textures.Add(ModContent.Request<Texture2D>("Assets/FilterAll"));
				texts.Add(Language.GetText("Mods.MagicStorage.FilterRecent"));
			}

			return new UIButtonChoice(onChanged, textures.ToArray(), texts.ToArray());
		}
	}
}
