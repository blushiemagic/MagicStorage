using System;
using System.Collections.Generic;
using MagicStorageExtra.UI;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;

namespace MagicStorageExtra
{
	public class GUIHelpers
	{
		public static UIButtonChoice MakeSortButtons(Action onChanged)
		{
			return new UIButtonChoice(onChanged,
				new[]
				{
					Main.inventorySortTexture[0],
					MagicStorageExtra.Instance.GetTexture("Assets/SortID"),
					MagicStorageExtra.Instance.GetTexture("Assets/SortName"),
					MagicStorageExtra.Instance.GetTexture("Assets/SortNumber"),
					MagicStorageExtra.Instance.GetTexture("Assets/SortNumber")
				},
				new[]
				{
					Language.GetText("Mods.MagicStorageExtra.SortDefault"),
					Language.GetText("Mods.MagicStorageExtra.SortID"),
					Language.GetText("Mods.MagicStorageExtra.SortName"),
					Language.GetText("Mods.MagicStorageExtra.SortValue"),
					Language.GetText("Mods.MagicStorageExtra.SortDps")
				});
		}

		public static UIButtonChoice MakeFilterButtons(bool withHistory, Action onChanged)
		{
			var textures = new List<Texture2D>
			{
				MagicStorageExtra.Instance.GetTexture("Assets/FilterAll"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterMelee"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterRanged"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterMagic"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterSummon"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterThrowing"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterAmmo"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterPickaxe"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterArmor"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterEquips"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterVanity"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterPotion"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterTile"),
				MagicStorageExtra.Instance.GetTexture("Assets/FilterMisc")
			};
			var texts = new List<LocalizedText>
			{
				Language.GetText("Mods.MagicStorageExtra.FilterAll"),
				Language.GetText("Mods.MagicStorageExtra.FilterWeaponsMelee"),
				Language.GetText("Mods.MagicStorageExtra.FilterWeaponsRanged"),
				Language.GetText("Mods.MagicStorageExtra.FilterWeaponsMagic"),
				Language.GetText("Mods.MagicStorageExtra.FilterWeaponsSummon"),
				Language.GetText("Mods.MagicStorageExtra.FilterWeaponsThrown"),
				Language.GetText("Mods.MagicStorageExtra.FilterAmmo"),
				Language.GetText("Mods.MagicStorageExtra.FilterTools"),
				Language.GetText("Mods.MagicStorageExtra.FilterArmor"),
				Language.GetText("Mods.MagicStorageExtra.FilterEquips"),
				Language.GetText("Mods.MagicStorageExtra.FilterVanity"),
				Language.GetText("Mods.MagicStorageExtra.FilterPotions"),
				Language.GetText("Mods.MagicStorageExtra.FilterTiles"),
				Language.GetText("Mods.MagicStorageExtra.FilterMisc")
			};
			if (withHistory)
			{
				textures.Add(MagicStorageExtra.Instance.GetTexture("Assets/FilterAll"));
				texts.Add(Language.GetText("Mods.MagicStorageExtra.FilterRecent"));
			}

			return new UIButtonChoice(onChanged, textures.ToArray(), texts.ToArray());
		}
	}
}