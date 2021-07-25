using System;
using System.Collections.Generic;
using MagicStorage.UI;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;

namespace MagicStorage
{
	public class GUIHelpers
	{
		public static UIButtonChoice MakeSortButtons(Action onChanged)
		{
			return new UIButtonChoice(onChanged, new[]
			{
				Main.inventorySortTexture[0],
				MagicStorage.Instance.GetTexture("Assets/SortID"),
				MagicStorage.Instance.GetTexture("Assets/SortName"),
				MagicStorage.Instance.GetTexture("Assets/SortNumber"),
				MagicStorage.Instance.GetTexture("Assets/SortNumber")
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
			var textures = new List<Texture2D>
			{
				MagicStorage.Instance.GetTexture("Assets/FilterAll"),
				MagicStorage.Instance.GetTexture("Assets/FilterMelee"),
				MagicStorage.Instance.GetTexture("Assets/FilterRanged"),
				MagicStorage.Instance.GetTexture("Assets/FilterMagic"),
				MagicStorage.Instance.GetTexture("Assets/FilterSummon"),
				MagicStorage.Instance.GetTexture("Assets/FilterThrowing"),
				MagicStorage.Instance.GetTexture("Assets/FilterAmmo"),
				MagicStorage.Instance.GetTexture("Assets/FilterPickaxe"),
				MagicStorage.Instance.GetTexture("Assets/FilterArmor"),
				MagicStorage.Instance.GetTexture("Assets/FilterEquips"),
				MagicStorage.Instance.GetTexture("Assets/FilterVanity"),
				MagicStorage.Instance.GetTexture("Assets/FilterPotion"),
				MagicStorage.Instance.GetTexture("Assets/FilterTile"),
				MagicStorage.Instance.GetTexture("Assets/FilterMisc")
			};
			var texts = new List<LocalizedText>
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
				textures.Add(MagicStorage.Instance.GetTexture("Assets/FilterAll"));
				texts.Add(Language.GetText("Mods.MagicStorage.FilterRecent"));
			}

			return new UIButtonChoice(onChanged, textures.ToArray(), texts.ToArray());
		}
	}
}
