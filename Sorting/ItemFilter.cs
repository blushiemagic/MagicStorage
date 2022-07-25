using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Sorting
{
	public static class ItemFilter
	{
		public delegate bool Filter(Item item);

		public static FilterMode GetFilter(int choice) {
			if (MagicStorageConfig.ExtraFilterIcons) {
				return (FilterMode)choice;
			} else {
				return choice switch {
					0 => FilterMode.All,
					1 => FilterMode.WeaponsMelee,
					2 => FilterMode.Tools,
					3 => FilterMode.Armor,
					4 => FilterMode.Potions,
					5 => FilterMode.Placeables,
					6 => FilterMode.Misc,
					_ => FilterMode.All  //Default to All
				};
			}
		}

		public static readonly Filter All = item => true;

		public static readonly Filter Weapon = item =>
			!(item.consumable && item.CountsAsClass(DamageClass.Throwing)) &&
			(item.damage > 0 || (item.CountsAsClass(DamageClass.Magic) && item.healLife > 0 && item.mana > 0)) &&
			item.pick == 0 &&
			item.axe == 0 &&
			item.hammer == 0;

		public static readonly Filter WeaponMelee = item =>
			(item.CountsAsClass(DamageClass.Melee) || (item.CountsAsClass(DamageClass.Throwing) && !item.consumable)) &&
			item.pick == 0 &&
			item.axe == 0 &&
			item.hammer == 0 &&
			item.damage > 0;

		public static readonly Filter WeaponRanged = item =>
			item.CountsAsClass(DamageClass.Ranged) && item.damage > 0 && item.ammo <= 0 && !WeaponThrown(item);

		public static readonly Filter WeaponMagic = item =>
			(item.CountsAsClass(DamageClass.Magic) || item.mana > 0) && !item.CountsAsClass(DamageClass.Summon) && !item.consumable;

		public static readonly Filter WeaponSummon = item => item.type switch
		{
			ItemID.LifeCrystal => false,
			ItemID.ManaCrystal => false,
			ItemID.CellPhone   => false,
			ItemID.PDA         => false,
			ItemID.MagicMirror => false,
			ItemID.IceMirror   => false,
			_                  => item.CountsAsClass(DamageClass.Summon) || SortClassList.BossSpawn(item) || SortClassList.Cart(item) || SortClassList.LightPet(item) || SortClassList.Mount(item) || item.sentry,
		};

		public static readonly Filter WeaponThrown = item => item.type switch
		{
			ItemID.Dynamite       => true,
			ItemID.StickyDynamite => true,
			ItemID.BouncyDynamite => true,
			ItemID.Bomb           => true,
			ItemID.StickyBomb     => true,
			ItemID.BouncyBomb     => true,
			_                     => (item.CountsAsClass(DamageClass.Throwing) && item.damage > 0) || (item.consumable && item.Name.ToLowerInvariant().EndsWith(" coating")),
		};

		public static readonly Filter Ammo = item =>
			item.ammo > 0 && item.damage > 0 && item.ammo != AmmoID.Coin;

		public static readonly Filter Tool = item =>
			item.pick > 0 || item.axe > 0 || item.hammer > 0;

		public static readonly Filter Armor = item =>
			!item.vanity && (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0);

		public static readonly Filter Vanity = item =>
			item.vanity || SortClassList.Dye(item) || SortClassList.HairDye(item) || SortClassList.VanityPet(item);

		public static readonly Filter Equipment = item =>
			!item.vanity &&
			(item.accessory || Main.projHook[item.shoot] || item.mountType >= 0 || (item.buffType > 0 && (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType])));

		public static readonly Filter ArmorAndEquipment = item => Armor(item) || Equipment(item);

		public static readonly Filter Potion = item => {
			bool mightBeAPotion = item.healLife > 0 || item.healMana > 0 || item.buffType > 0 || item.potion;

			if (!mightBeAPotion)
				return false;  //Definitely not a "potion"

			//It's a consumable item and it plays the sound for food (Item2) or drinks (Item3), so just assume that it is a "potion"
			return item.consumable && (item.UseSound == SoundID.Item2 || item.UseSound == SoundID.Item3);
		};

		public static readonly Filter Placeable = item =>
			item.createTile >= TileID.Dirt || item.createWall > 0;

		public static readonly Filter Misc = item =>
			blacklist.All(filter => !filter(item));

		private static readonly Filter[] blacklist =
		{
			WeaponMelee,
			WeaponRanged,
			WeaponMagic,
			WeaponSummon,
			WeaponThrown,
			Ammo,
			WeaponThrown,
			Vanity,
			Tool,
			Armor,
			Equipment,
			Potion,
			Placeable,
		};
	}
}
