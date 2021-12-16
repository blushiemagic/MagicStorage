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
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortID", AssetRequestMode.ImmediateLoad),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortName", AssetRequestMode.ImmediateLoad),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortNumber", AssetRequestMode.ImmediateLoad),
				MagicStorage.Instance.Assets.Request<Texture2D>("Assets/SortNumber", AssetRequestMode.ImmediateLoad)
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
			List<Asset<Texture2D>> textures = MagicStorageConfig.ExtraFilterIcons
				? new()
				{
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterAll", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMelee", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterRanged", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMagic", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterSummon", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterThrowing", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterAmmo", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterPickaxe", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterArmor", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterEquips", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterVanity", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterPotion", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterTile", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMisc", AssetRequestMode.ImmediateLoad)
				}
				: new()
				{
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterAll", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMelee", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterPickaxe", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterArmor", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterPotion", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterTile", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMisc", AssetRequestMode.ImmediateLoad)
				};
			List<LocalizedText> texts = MagicStorageConfig.ExtraFilterIcons
				? new()
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
				}
				: new()
				{
					Language.GetText("Mods.MagicStorage.FilterAll"),
					Language.GetText("Mods.MagicStorage.FilterWeapons"),
					Language.GetText("Mods.MagicStorage.FilterTools"),
					Language.GetText("Mods.MagicStorage.FilterEquips"),
					Language.GetText("Mods.MagicStorage.FilterPotions"),
					Language.GetText("Mods.MagicStorage.FilterTiles"),
					Language.GetText("Mods.MagicStorage.FilterMisc")
				};
			if (withHistory)
			{
				textures.Add(MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterAll", AssetRequestMode.ImmediateLoad));
				texts.Add(Language.GetText("Mods.MagicStorage.FilterRecent"));
			}

			return new UIButtonChoice(onChanged, textures.ToArray(), texts.ToArray(),
				MagicStorageConfig.ExtraFilterIcons ? 21 : 32,
				MagicStorageConfig.ExtraFilterIcons ? 1 : 8);
		}
	}
}
