using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;

namespace MagicStorage
{
	public class GUIHelpers
	{

		public static UIButtonChoice MakeSortButtons(Action onChanged) {
			return new UIButtonChoice(onChanged, new[] {
				Main.inventorySortTexture[0],
				MagicStorage.Instance.GetTexture("SortID"),
				MagicStorage.Instance.GetTexture("SortName"),
				MagicStorage.Instance.GetTexture("SortNumber"),
				MagicStorage.Instance.GetTexture("SortNumber")
			}, new[] {
				Language.GetText("Mods.MagicStorage.SortDefault"),
				Language.GetText("Mods.MagicStorage.SortID"),
				Language.GetText("Mods.MagicStorage.SortName"),
				Language.GetText("Mods.MagicStorage.SortValue"),
				Language.GetText("Mods.MagicStorage.SortDps")
			});
		}

		public static UIButtonChoice MakeFilterButtons(bool withHistory, Action onChanged) {
			var textures = new List<Texture2D> {
				MagicStorage.Instance.GetTexture("FilterAll"),
				MagicStorage.Instance.GetTexture("FilterMelee"),
				MagicStorage.Instance.GetTexture("FilterRanged"),
				MagicStorage.Instance.GetTexture("FilterMagic"),
				MagicStorage.Instance.GetTexture("FilterSummon"),
				MagicStorage.Instance.GetTexture("FilterThrowing"),
				MagicStorage.Instance.GetTexture("FilterThrowing"),
				MagicStorage.Instance.GetTexture("FilterPickaxe"),
				MagicStorage.Instance.GetTexture("FilterArmor"),
				MagicStorage.Instance.GetTexture("FilterArmor"),
				MagicStorage.Instance.GetTexture("FilterArmor"),
				MagicStorage.Instance.GetTexture("FilterPotion"),
				MagicStorage.Instance.GetTexture("FilterTile"),
				MagicStorage.Instance.GetTexture("FilterMisc")
			};
			var texts = new List<LocalizedText> {
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
			if (withHistory) {
				textures.Add(MagicStorage.Instance.GetTexture("FilterAll"));
				texts.Add(Language.GetText("Mods.MagicStorage.FilterRecent"));
			}
			return new UIButtonChoice(onChanged, textures.ToArray(), texts.ToArray());
		}
	}
}
