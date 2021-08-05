CountsAsClass(using System;
using System.Collections.Generic;
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
            if (obj is Item item)
            {
                return Passes(item);
            }
            if (obj is Recipe recipe)
            {
                return Passes(recipe.createItem);
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

    public class FilterMelee : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return item.CountsAsClass(DamageClass.Melee) && item.pick == 0 && item.axe == 0 && item.hammer == 0;
        }
    }

    public class FilterRanged : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return item.CountsAsClass(DamageClass.Ranged);
        }
    }

    public class FilterMagic : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return item.CountsAsClass(DamageClass.Magic);
        }
    }

    public class FilterSummon : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return item.CountsAsClass(DamageClass.Summon);
        }
    }

    public class FilterThrown : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return item.CountsAsClass(DamageClass.Throwing);
        }
    }

    public class FilterOtherWeapon : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return !item.CountsAsClass(DamageClass.Melee) &&
                   !item.CountsAsClass(DamageClass.Ranged) &&
                   !item.CountsAsClass(DamageClass.Magic) &&
                   !item.CountsAsClass(DamageClass.Summon) &&
                   !item.CountsAsClass(DamageClass.Throwing) &&
                   item.damage > 0;
        }
    }

    public class FilterWeapon : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return item.damage > 0 && item.pick == 0 && item.axe == 0 && item.hammer == 0;
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

    public class FilterEquipment : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0 || item.accessory || Main.projHook[item.shoot] || item.mountType >= 0 || (item.buffType > 0 && (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType]));
        }
    }

    public class FilterPotion : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return item.consumable && (item.healLife > 0 || item.healMana > 0 || item.buffType > 0);
        }
    }

    public class FilterPlaceable : ItemFilter
    {
        public override bool Passes(Item item)
        {
            return item.createTile >= TileID.Dirt || item.createWall > 0;
        }
    }

    public class FilterMisc : ItemFilter
    {
        private static readonly List<ItemFilter> blacklist = new List<ItemFilter> {
            new FilterWeapon(),
            new FilterTool(),
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
