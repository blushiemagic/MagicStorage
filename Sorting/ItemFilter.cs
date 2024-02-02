using MagicStorage.Common.Systems;
using MagicStorage.CrossMod;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Sorting
{
	public static class ItemFilter
	{
		public delegate bool Filter(Item item);

		public static readonly Filter All = item => true;

		public static readonly Filter Weapon = item => item.DamageType != DamageClass.Default && item.damage > 0 && item.ammo == 0 && !Tool(item);

		public static readonly Filter WeaponMelee = item => item.DamageType.CountsAsClass(DamageClass.Melee) && Weapon(item);

		public static readonly Filter WeaponRanged = item => item.DamageType.CountsAsClass(DamageClass.Ranged) && Weapon(item);

		public static readonly Filter WeaponMagic = item => item.DamageType.CountsAsClass(DamageClass.Magic) && Weapon(item);

		public static readonly Filter WeaponSummon = item => item.type switch
		{
			ItemID.LifeCrystal => false,
			ItemID.ManaCrystal => false,
			ItemID.CellPhone   => false,
			ItemID.PDA         => false,
			ItemID.MagicMirror => false,
			ItemID.IceMirror   => false,
			ItemID.TreasureMap => false,
			_                  => item.CountsAsClass(DamageClass.Summon) || item.sentry,
		};

		public static readonly Filter WeaponThrown = item => item.type switch
		{
			ItemID.Dynamite       => true,
			ItemID.StickyDynamite => true,
			ItemID.BouncyDynamite => true,
			ItemID.Bomb           => true,
			ItemID.StickyBomb     => true,
			ItemID.BouncyBomb     => true,
			_                     => item.CountsAsClass(DamageClass.Throwing) && item.damage > 0 && (item.ammo == 0 || item.notAmmo) && item.shoot > ProjectileID.None
		};

		public static readonly Filter WeaponOther = item => !FilteringOptionLoader.Options.Where(o => !object.ReferenceEquals(o, FilteringOptionLoader.Definitions.Weapon) && o.FiltersDamageClass).Any(o => o.Filter(item)) && Weapon(item);

		public static readonly Filter Ammo = item =>
			item.ammo > 0 && item.damage > 0 && item.ammo != AmmoID.Coin;

		public static readonly Filter Tool = item =>
			item.pick > 0 || item.axe > 0 || item.hammer > 0;

		public static readonly Filter Fishing = item => item.type switch {
			ItemID.AnglerHat => true,
			ItemID.AnglerVest => true,
			ItemID.AnglerPants => true,
			ItemID.AnglerTackleBag => true,
			ItemID.LavaproofTackleBag => true,
			ItemID.AnglerEarring => true,
			ItemID.FloatingTube => true,
			ItemID.HighTestFishingLine => true,
			ItemID.TackleBox => true,
			ItemID.FishermansGuide => true,
			ItemID.WeatherRadio => true,
			ItemID.Sextant => true,
			ItemID.FishFinder => true,
			ItemID.ChumBucket => true,
			ItemID.GummyWorm => true,
			ItemID.MolluskWhistle => true,
			ItemID.FishingPotion => true,
			ItemID.CratePotion => true,
			ItemID.SonarPotion => true,
			ItemID.Ale => true,
			_ when MagicRecipes.fishingBobberRecipeGroup.ValidItems.Contains(item.type) => true,
			_ when MagicRecipes.toiletRecipeGroup.ValidItems.Contains(item.type) => true,
			_ => SortClassList.FishingPole(item) || SortClassList.FishingBait(item)
		};

		public static readonly Filter ToolsAndFishing = item => Tool(item) || SortClassList.FishingPole(item);

		public static readonly Filter Armor = item =>
			!item.vanity && (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0);

		public static readonly Filter Vanity = item =>
			item.vanity || SortClassList.Dye(item) || SortClassList.HairDye(item) || SortClassList.VanityPet(item);

		public static readonly Filter Equipment = item =>
			!item.vanity &&
			(item.accessory || Main.projHook[item.shoot] || item.mountType >= 0 || SortClassList.Cart(item) || SortClassList.VanityPet(item) || SortClassList.LightPet(item) || SortClassList.Mount(item));

		public static readonly Filter ArmorAndEquipment = item => Armor(item) || Equipment(item);

		public static readonly Filter Potion = item => {
			bool mightBeAPotion = item.healLife > 0 || item.healMana > 0 || item.buffType > 0 || item.potion;

			if (!mightBeAPotion)
				return false;  //Definitely not a "potion"

			//It's a consumable item and it plays the sound for food (Item2) or drinks (Item3), so just assume that it is a "potion"
			return item.consumable && item.UseSound is SoundStyle style && (style.IsTheSameAs(SoundID.Item2) || style.IsTheSameAs(SoundID.Item3));
		};

		public static readonly Filter Placeable = item =>
			item.createTile >= TileID.Dirt || item.createWall > 0;

		//A typical "material" item like Gel or Fallen Stars
		//Filter out equipment, items that place tiles/walls, weapons, tools, dyes and paint
		public static readonly Filter Material = item =>
			item.material
			&& !ArmorAndEquipment(item)
			&& !Vanity(item)
			&& !Placeable(item)
			&& !Weapon(item)
			&& item.useStyle == ItemUseStyleID.None
			&& !Tool(item)
			&& item.dye <= 0
			&& item.paint <= 0;

		public static readonly Filter MiscGameplayItem = SortClassList.BossSpawn;

		public static readonly Filter Misc = item =>
			!blacklist.Any(filter => filter(item));

		public static readonly Filter Unstackable = item => item.maxStack == 1;

		public static readonly Filter Stackable = item => item.maxStack > 1;

		public static readonly Filter FullyResearched = item => Utility.IsFullyResearched(item.type, mustBeResearchable: false);

		public static readonly Filter NotFullyResearched = item => !Utility.IsFullyResearched(item.type, mustBeResearchable: false);

		private static readonly Filter[] blacklist =
		{
			Weapon,
			Ammo,
			Vanity,
			Tool,
			Armor,
			Equipment,
			Potion,
			Placeable,
			MiscGameplayItem
		};
	}
}
