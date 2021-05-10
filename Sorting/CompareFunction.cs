using System;
using System.Collections.Generic;
using Terraria;

namespace MagicStorageExtra.Sorting
{
	public abstract class CompareFunction : IComparer<Item>
	{
		public abstract int Compare(Item item1, Item item2);

		public int Compare(object object1, object object2) {
			switch (object1) {
				case Item item1 when object2 is Item item2:
					return Compare(item1, item2);
				case Recipe recipe1 when object2 is Recipe recipe2:
					return Compare(recipe1.createItem, recipe2.createItem);
				default:
					return 0;
			}
		}
	}

	public class CompareID : CompareFunction
	{
		public override int Compare(Item item1, Item item2) => item1.type - item2.type;
	}

	public class CompareName : CompareFunction
	{
		public override int Compare(Item item1, Item item2) => string.Compare(item1.Name, item2.Name, StringComparison.OrdinalIgnoreCase);
	}

	public class CompareQuantity : CompareFunction
	{
		public override int Compare(Item item1, Item item2) =>
			(int)Math.Ceiling(item2.stack / (float)item2.maxStack) - (int)Math.Ceiling(item1.stack / (float)item1.maxStack);
	}

	public class CompareValue : CompareFunction
	{
		public override int Compare(Item item1, Item item2) => item2.value - item1.value;
	}

	public class CompareDps : CompareFunction
	{
		public override int Compare(Item item1, Item item2) => (int)((GetDps(item2) - GetDps(item1)) * 100);

		public static double GetDps(Item item) {
			if (item.damage <= 0) return 0d;
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

			return Math.Max(item.damage - defence * 0.5d, 1) / Math.Max((item.useTime + item.reuseDelay) / 60d, 0.001d) * (1d + item.crit / 100d);
		}
	}
}
