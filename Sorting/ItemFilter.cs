using System;
using System.Collections.Generic;
using MagicStorage.Components;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.Sorting
{
	public abstract class ItemFilter
	{
		public abstract bool Passes(Item item);

		public bool Passes(object obj)
		{
			if (obj is Item)
			{
				return Passes((Item)obj);
			}
			if (obj is Recipe)
			{
				return Passes(((Recipe)obj).createItem);
			}
			return false;
		}
	}

	public class FilterAll : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return true;
		}
	}

	public class FilterWeaponMelee : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return (item.melee || (item.thrown && !item.consumable)) && item.pick == 0 && item.axe == 0 && item.hammer == 0 && item.damage > 0;
		}
	}

	public class FilterWeaponRanged : ItemFilter
	{
        readonly FilterWeaponThrown _thrown = new FilterWeaponThrown();
        
		public override bool Passes(Item item)
		{
			return item.ranged && item.damage > 0 && item.ammo <= 0 && !_thrown.Passes(item);
		}
	}

	public class FilterWeaponMagic : ItemFilter
	{
		public override bool Passes(Item item)
		{
		    return (item.magic || item.mana > 0) && !item.summon && !item.consumable;
		}
	}

	public class FilterWeaponSummon : ItemFilter
	{
		public override bool Passes(Item item)
		{
		    switch (item.type)
		    {
                case 109:// mana or heart crystal
                case 29:
                case ItemID.CellPhone:
                case ItemID.PDA:
                case ItemID.MagicMirror:
                case ItemID.IceMirror:
                    return false; 
		    }
            
			return item.summon || SortClassList.BossSpawn(item) || SortClassList.Cart(item) || SortClassList.LightPet(item) || SortClassList.Mount(item) || item.sentry;
		}
	}

	public class FilterWeaponThrown : ItemFilter
	{
		public override bool Passes(Item item)
		{
		    switch (item.type)
		    {
                case 167: // dynamite
                case 3547: 
                case 2896: 
                case 166: // bomb
                case 235: 
                case 3115:
                    return true;
		    }
			return (item.thrown && item.damage > 0) || (item.consumable && item.Name.ToLowerInvariant().EndsWith(" coating"));
		}
	}

	public class FilterAmmo : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return item.ammo > 0 && item.damage > 0 && item.ammo != AmmoID.Coin;
		}
	}

	public class FilterVanity : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return item.vanity || SortClassList.Dye(item) || SortClassList.HairDye(item) || SortClassList.VanityPet(item);
		}
	}

	public class FilterOtherWeapon : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return !item.melee && !item.ranged && !item.magic && !item.summon && !item.thrown && item.damage > 0;
		}
	}

	public class FilterWeapon : ItemFilter
	{
		public override bool Passes(Item item)
	    {
	        return !(item.consumable && item.thrown) && (item.damage > 0 || (item.magic && item.healLife > 0 && item.mana > 0)) && item.pick == 0 && item.axe == 0 && item.hammer == 0;
	    }
	}

	public class FilterPickaxe : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return item.pick > 0;
		}
	}

	public class FilterAxe : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return item.axe > 0;
		}
	}

	public class FilterHammer : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return item.hammer > 0;
		}
	}

	public class FilterTool : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return item.pick > 0 || item.axe > 0 || item.hammer > 0;
		}
	}

	public class FilterArmor : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return !item.vanity && (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0);
		}
	}

	public class FilterEquipment : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return !item.vanity && (item.accessory || Main.projHook[item.shoot] || item.mountType >= 0 || (item.buffType > 0 && (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType])));
		}
	}

	public class FilterPotion : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return item.consumable && (item.healLife > 0 || item.healMana > 0 || item.buffType > 0 || item.potion || item.Name.ToLowerInvariant().Contains("potion") || item.Name.ToLowerInvariant().Contains("elixir"));
		}
	}

	public class FilterPlaceable : ItemFilter
	{
		public override bool Passes(Item item)
		{
			return item.createTile >= 0 || item.createWall > 0;
		}
	}

	public class FilterMisc : ItemFilter
	{
		private static List<ItemFilter> blacklist = new List<ItemFilter> {
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
			foreach (var filter in blacklist)
			{
				if (filter.Passes(item))
				{
					return false;
				}
			}
			return true;
		}
	}
}