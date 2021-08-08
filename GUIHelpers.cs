using System;
using System.Collections.Generic;
using MagicStorage.UI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage
{
	public static class GUIHelpers
	{
		public static UIButtonChoice MakeSortButtons(Action onChanged)
		{
			return new UIButtonChoice(onChanged, new[]
			{
				TextureAssets.InventorySort[0],
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortID"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortName"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortNumber"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortNumber")
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
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterAll"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMelee"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterRanged"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMagic"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterSummon"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterThrowing"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterAmmo"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterPickaxe"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterArmor"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterEquips"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterVanity"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterPotion"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterTile"),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMisc")
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
				textures.Add(MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterAll"));
				texts.Add(Language.GetText("Mods.MagicStorage.FilterRecent"));
			}

			return new UIButtonChoice(onChanged, textures.ToArray(), texts.ToArray());
		}
	}
}
