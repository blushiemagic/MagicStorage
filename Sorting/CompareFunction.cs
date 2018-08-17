using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.Sorting
{
    public abstract class CompareFunction : IComparer<Item>
	{
		public abstract int Compare(Item item1, Item item2);

		public int Compare(object object1, object object2)
		{
			if (object1 is Item && object2 is Item)
			{
				return Compare((Item)object1, (Item)object2);
			}
			if (object1 is Recipe && object2 is Recipe)
			{
				return Compare(((Recipe)object1).createItem, ((Recipe)object2).createItem);
			}
			return 0;
		}
	}

	public class CompareID : CompareFunction
	{
		public override int Compare(Item item1, Item item2)
		{
			return item1.type - item2.type;
		}
	}

	public class CompareName : CompareFunction
	{
		public override int Compare(Item item1, Item item2)
		{
			return string.Compare(item1.Name, item2.Name, StringComparison.OrdinalIgnoreCase);
		}
	}

	public class CompareQuantity : CompareFunction
	{
		public override int Compare(Item item1, Item item2)
		{
			return (int)Math.Ceiling((float)item2.stack / (float)item2.maxStack) - (int)Math.Ceiling((float)item1.stack / (float)item1.maxStack);
		}
	}

	public class CompareValue : CompareFunction
	{
		public override int Compare(Item item1, Item item2)
		{
            return item1.value - item2.value;
		}
	}

	public class CompareDps : CompareFunction
	{
        public override int Compare(Item item1, Item item2)
        {
            return (int)((GetDps(item1) - GetDps(item2)) * 100);
        }

        public static float GetDps(Item item)
        {
            if (item.damage <= 0) return 0f;
            int defence;
            if (NPC.downedMoonlord)
                defence = 50;
            else if (NPC.downedAncientCultist)
                defence = 43;
            else if (NPC.downedGolemBoss)
                defence = 38;
            else if (NPC.downedPlantBoss)
                defence = 32;
            else if (NPC.downedMechBossAny)
                defence = 26;
            else if (Main.hardMode)
                defence = 22;
            else if (NPC.downedBoss3)
                defence = 16;
            else if (NPC.downedBoss2)
                defence = 14;
            else if (NPC.downedBoss1)
                defence = 10;
            else
                defence = 8;

            return Math.Max(item.damage - defence * 0.5f, 1) / Math.Max((item.useTime + item.reuseDelay) / 60f, 0.001f);
        }
	}
}