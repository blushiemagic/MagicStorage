using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MagicStorage.Sorting
{
	public abstract class ItemFilter
	{
		public abstract bool Passes(Item item);

		public bool Passes(object obj)
		{
			return obj switch
			{
				Item item     => Passes(item),
				Recipe recipe => Passes(recipe.createItem),
				_             => false
			};
		}
	}

	public class FilterAll : ItemFilter
	{
		public override bool Passes(Item item) => true;
	}

	public class FilterWeaponMelee : ItemFilter
	{
		public override bool Passes(Item item) =>
			(item.CountsAsClass(DamageClass.Melee) || item.CountsAsClass(DamageClass.Throwing) && !item.consumable) &&
			item.pick == 0 &&
			item.axe == 0 &&
			item.hammer == 0 &&
			item.damage > 0;
	}

	public class FilterWeaponRanged : ItemFilter
	{
		private readonly FilterWeaponThrown thrown = new();

		public override bool Passes(Item item) => item.CountsAsClass(DamageClass.Ranged) && item.damage > 0 && item.ammo <= 0 && !thrown.Passes(item);
	}

	public class FilterWeaponMagic : ItemFilter
	{
		public override bool Passes(Item item) => (item.CountsAsClass(DamageClass.Magic) || item.mana > 0) && !item.CountsAsClass(DamageClass.Summon) && !item.consumable;
	}

	public class FilterWeaponSummon : ItemFilter
	{
		public override bool Passes(Item item)
		{
			switch (item.type)
			{
				case ItemID.LifeCrystal:
				case ItemID.ManaCrystal:
				case ItemID.CellPhone:
				case ItemID.PDA:
				case ItemID.MagicMirror:
				case ItemID.IceMirror:
					return false;
			}

			return item.CountsAsClass(DamageClass.Summon) ||
				   SortClassList.BossSpawn(item) ||
				   SortClassList.Cart(item) ||
				   SortClassList.LightPet(item) ||
				   SortClassList.Mount(item) ||
				   item.sentry;
		}
	}

	public class FilterWeaponThrown : ItemFilter
	{
		public override bool Passes(Item item)
		{
			switch (item.type)
			{
				case ItemID.Dynamite:
				case ItemID.StickyDynamite:
				case ItemID.BouncyDynamite:
				case ItemID.Bomb:
				case ItemID.StickyBomb:
				case ItemID.BouncyBomb:
					return true;
			}

			return item.CountsAsClass(DamageClass.Throwing) && item.damage > 0 || item.consumable && item.Name.ToLowerInvariant().EndsWith(" coating");
		}
	}

	public class FilterAmmo : ItemFilter
	{
		public override bool Passes(Item item) => item.ammo > 0 && item.damage > 0 && item.ammo != AmmoID.Coin;
	}

	public class FilterVanity : ItemFilter
	{
		public override bool Passes(Item item) => item.vanity || SortClassList.Dye(item) || SortClassList.HairDye(item) || SortClassList.VanityPet(item);
	}

	public class FilterOtherWeapon : ItemFilter
	{
		public override bool Passes(Item item) =>
			!item.CountsAsClass(DamageClass.Melee) &&
			!item.CountsAsClass(DamageClass.Ranged) &&
			!item.CountsAsClass(DamageClass.Magic) &&
			!item.CountsAsClass(DamageClass.Summon) &&
			!item.CountsAsClass(DamageClass.Throwing) &&
			item.damage > 0;
	}

	public class FilterWeapon : ItemFilter
	{
		public override bool Passes(Item item) =>
			!(item.consumable && item.CountsAsClass(DamageClass.Throwing)) &&
			(item.damage > 0 || item.CountsAsClass(DamageClass.Magic) && item.healLife > 0 && item.mana > 0) &&
			item.pick == 0 &&
			item.axe == 0 &&
			item.hammer == 0;
	}

	public class FilterPickaxe : ItemFilter
	{
		public override bool Passes(Item item) => item.pick > 0;
	}

	public class FilterAxe : ItemFilter
	{
		public override bool Passes(Item item) => item.axe > 0;
	}

	public class FilterHammer : ItemFilter
	{
		public override bool Passes(Item item) => item.hammer > 0;
	}

	public class FilterTool : ItemFilter
	{
		public override bool Passes(Item item) => item.pick > 0 || item.axe > 0 || item.hammer > 0;
	}

	public class FilterArmor : ItemFilter
	{
		public override bool Passes(Item item) => !item.vanity && (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0);
	}

	public class FilterEquipment : ItemFilter
	{
		public override bool Passes(Item item) =>
			!item.vanity &&
			(item.accessory || Main.projHook[item.shoot] || item.mountType >= 0 || item.buffType > 0 && (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType]));
	}

	public class FilterPotion : ItemFilter
	{
		public override bool Passes(Item item) =>
			item.consumable &&
			(item.healLife > 0 ||
			 item.healMana > 0 ||
			 item.buffType > 0 ||
			 item.potion ||
			 item.Name.ToLowerInvariant().Contains("potion") ||
			 item.Name.ToLowerInvariant().Contains("elixir"));
	}

	public class FilterPlaceable : ItemFilter
	{
		public override bool Passes(Item item) => item.createTile >= TileID.Dirt || item.createWall > 0;
	}

	public class FilterMisc : ItemFilter
	{
		private static readonly List<ItemFilter> blacklist = new()
		{
			new FilterWeaponMelee(),
			new FilterWeaponRanged(),
			new FilterWeaponMagic(),
			new FilterWeaponSummon(),
			new FilterWeaponThrown(),
			new FilterAmmo(),
			new FilterWeaponThrown(),
			new FilterVanity(),
			new FilterTool(),
			new FilterArmor(),
			new FilterEquipment(),
			new FilterPotion(),
			new FilterPlaceable()
		};

		public override bool Passes(Item item)
		{
			return blacklist.All(filter => !filter.Passes(item));
		}
	}
}
