using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;

namespace MagicStorageExtra
{
	public class GUIHelpers
	{

		public static UIButtonChoice MakeSortButtons(Action onChanged) {
			return new UIButtonChoice(onChanged,
				new[] {
					Main.inventorySortTexture[0],
					MagicStorageExtra.Instance.GetTexture("SortID"),
					MagicStorageExtra.Instance.GetTexture("SortName"),
					MagicStorageExtra.Instance.GetTexture("SortNumber"),
					MagicStorageExtra.Instance.GetTexture("SortNumber")
				},
				new[] {
					Language.GetText("Mods.MagicStorageExtra.SortDefault"),
					Language.GetText("Mods.MagicStorageExtra.SortID"),
					Language.GetText("Mods.MagicStorageExtra.SortName"),
					Language.GetText("Mods.MagicStorageExtra.SortValue"),
					Language.GetText("Mods.MagicStorageExtra.SortDps")
				});
		}

		public static UIButtonChoice MakeFilterButtons(bool withHistory, Action onChanged) {
			var textures = new List<Texture2D> {
				MagicStorageExtra.Instance.GetTexture("FilterAll"),
				MagicStorageExtra.Instance.GetTexture("FilterMelee"),
				MagicStorageExtra.Instance.GetTexture("FilterRanged"),
				MagicStorageExtra.Instance.GetTexture("FilterMagic"),
				MagicStorageExtra.Instance.GetTexture("FilterSummon"),
				MagicStorageExtra.Instance.GetTexture("FilterThrowing"),
				MagicStorageExtra.Instance.GetTexture("FilterThrowing"),
				MagicStorageExtra.Instance.GetTexture("FilterPickaxe"),
				MagicStorageExtra.Instance.GetTexture("FilterArmor"),
				MagicStorageExtra.Instance.GetTexture("FilterArmor"),
				MagicStorageExtra.Instance.GetTexture("FilterArmor"),
				MagicStorageExtra.Instance.GetTexture("FilterPotion"),
				MagicStorageExtra.Instance.GetTexture("FilterTile"),
				MagicStorageExtra.Instance.GetTexture("FilterMisc")
			};
			var texts = new List<LocalizedText> {
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
			if (withHistory) {
				textures.Add(MagicStorageExtra.Instance.GetTexture("FilterAll"));
				texts.Add(Language.GetText("Mods.MagicStorageExtra.FilterRecent"));
			}
			return new UIButtonChoice(onChanged, textures.ToArray(), texts.ToArray());
		}
	}
}
