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
				Language.GetText("Mods.MagicStorage.Sort.Default"),
				Language.GetText("Mods.MagicStorage.Sort.ID"),
				Language.GetText("Mods.MagicStorage.Sort.Name"),
				Language.GetText("Mods.MagicStorage.Sort.Value"),
				Language.GetText("Mods.MagicStorage.Sort.Dps")
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
					Language.GetText("Mods.MagicStorage.Filters.All"),
					Language.GetText("Mods.MagicStorage.Filters.WeaponsMelee"),
					Language.GetText("Mods.MagicStorage.Filters.WeaponsRanged"),
					Language.GetText("Mods.MagicStorage.Filters.WeaponsMagic"),
					Language.GetText("Mods.MagicStorage.Filters.WeaponsSummon"),
					Language.GetText("Mods.MagicStorage.Filters.WeaponsThrown"),
					Language.GetText("Mods.MagicStorage.Filters.Ammo"),
					Language.GetText("Mods.MagicStorage.Filters.Tools"),
					Language.GetText("Mods.MagicStorage.Filters.Armor"),
					Language.GetText("Mods.MagicStorage.Filters.Equips"),
					Language.GetText("Mods.MagicStorage.Filters.Vanity"),
					Language.GetText("Mods.MagicStorage.Filters.Potions"),
					Language.GetText("Mods.MagicStorage.Filters.Tiles"),
					Language.GetText("Mods.MagicStorage.Filters.Misc")
				}
				: new()
				{
					Language.GetText("Mods.MagicStorage.Filters.All"),
					Language.GetText("Mods.MagicStorage.Filters.Weapons"),
					Language.GetText("Mods.MagicStorage.Filters.Tools"),
					Language.GetText("Mods.MagicStorage.Filters.Equips"),
					Language.GetText("Mods.MagicStorage.Filters.Potions"),
					Language.GetText("Mods.MagicStorage.Filters.Tiles"),
					Language.GetText("Mods.MagicStorage.Filters.Misc")
				};
			if (withHistory)
			{
				textures.Add(MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterAll", AssetRequestMode.ImmediateLoad));
				texts.Add(Language.GetText("Mods.MagicStorage.Filters.Recent"));
			}

			return new UIButtonChoice(onChanged, textures.ToArray(), texts.ToArray(),
				MagicStorageConfig.ExtraFilterIcons ? 21 : 32,
				MagicStorageConfig.ExtraFilterIcons ? 1 : 8);
		}
	}
}
