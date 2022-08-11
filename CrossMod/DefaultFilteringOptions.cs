using MagicStorage.Sorting;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod {
	[Autoload(false)]
	public sealed class FilterAll : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.All;

		public override string Texture => "MagicStorage/Assets/FilterAll";

		public override string Name => "All";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterWeapons : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Weapon;

		public override bool FiltersDamageClass => true;

		public override string Texture => "MagicStorage/Assets/FilterMelee";

		public override string Name => "Weapons";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterMelee : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.WeaponMelee;

		public override bool FiltersDamageClass => true;

		public override string Texture => "MagicStorage/Assets/FilterMelee";

		public override string Name => "WeaponsMelee";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterRanged : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.WeaponRanged;

		public override bool FiltersDamageClass => true;

		public override string Texture => "MagicStorage/Assets/FilterRanged";

		public override string Name => "WeaponsRanged";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterMagic : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.WeaponMagic;

		public override bool FiltersDamageClass => true;

		public override string Texture => "MagicStorage/Assets/FilterMagic";

		public override string Name => "WeaponsMagic";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterSummon : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.WeaponSummon;

		public override bool FiltersDamageClass => true;

		public override string Texture => "MagicStorage/Assets/FilterSummon";

		public override string Name => "WeaponsSummon";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterThrowing : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.WeaponThrown;

		public override bool FiltersDamageClass => true;

		public override string Texture => "MagicStorage/Assets/FilterThrowing";

		public override string Name => "WeaponsThrown";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterAmmo : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Ammo;

		public override string Texture => "MagicStorage/Assets/FilterAmmo";

		public override string Name => "Ammo";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterTools : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Tool;

		public override string Texture => "MagicStorage/Assets/FilterPickaxe";

		public override string Name => "Tools";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterArmorAndEquips : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.ArmorAndEquipment;

		public override string Texture => "MagicStorage/Assets/FilterAmorAndEquips";

		public override string Name => "ArmorAndEquips";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterArmor : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Armor;

		public override string Texture => "MagicStorage/Assets/FilterArmor";

		public override string Name => "Armor";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterEquips : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Equipment;

		public override string Texture => "MagicStorage/Assets/FilterEquips";

		public override string Name => "Equips";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterVanity : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Vanity;

		public override string Texture => "MagicStorage/Assets/FilterVanity";

		public override string Name => "Vanity";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterPotion : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Potion;

		public override string Texture => "MagicStorage/Assets/FilterPotion";

		public override string Name => "Potions";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterTiles : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Placeable;

		public override string Texture => "MagicStorage/Assets/FilterTile";

		public override string Name => "Tiles";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterMisc : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Misc;

		public override string Texture => "MagicStorage/Assets/FilterMisc";

		public override string Name => "Misc";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterRecent : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Misc;

		public override string Texture => "MagicStorage/Assets/FilterAll";

		public override string Name => "Recent";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order

		public override bool GetDefaultVisibility(bool craftingGUI) => !craftingGUI;
	}

	[Autoload(false)]
	public sealed class FilterOtherWeaponClasses : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.WeaponOther;

		public override bool FiltersDamageClass => false;  //Set to false to prevent recursion, even though it does filter damage classes

		public override string Texture => "MagicStorage/Assets/FilterOtherWeapon";

		public override string Name => "WeaponsOther";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterUnstackables : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Unstackable;

		public override string Texture => "MagicStorage/Assets/FilterMisc";

		public override string Name => "Unstackables";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterStackables : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.Stackable;

		public override string Texture => "MagicStorage/Assets/FilterMisc";

		public override string Name => "Stackables";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class FilterNotFullyResearched : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.NotFullyResearched;

		public override string Texture => "MagicStorage/Assets/FilterMisc";

		public override string Name => "NotFullyResearched";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order

		public override bool GetDefaultVisibility(bool craftingGUI) => Main.gameMenu || Main.LocalPlayer.difficulty == PlayerDifficultyID.Creative;
	}

	[Autoload(false)]
	public sealed class FilterFullyResearched : FilteringOption {
		public override ItemFilter.Filter Filter => ItemFilter.FullyResearched;

		public override string Texture => "MagicStorage/Assets/FilterMisc";

		public override string Name => "FullyResearched";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order

		public override bool GetDefaultVisibility(bool craftingGUI) => Main.gameMenu || Main.LocalPlayer.difficulty == PlayerDifficultyID.Creative;
	}
}
