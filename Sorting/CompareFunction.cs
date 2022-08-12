using System;
using System.Collections.Generic;
using Terraria;

namespace MagicStorage.Sorting
{
	public abstract class CompareFunction<T> : IComparer<Item>
		where T : CompareFunction<T>, new()
	{
		public static T Instance { get; } = new();

		public abstract int Compare(Item item1, Item item2);
	}

	public class CompareID : CompareFunction<CompareID>
	{
		// "item2" and "item1" are reversed here.
		// This is intentional!
		// Magic Storage puts vanilla items above modded items in the sorting list, so the reversing here is needed.
		//  -- absoluteAquarian
		public override int Compare(Item item1, Item item2) => item2.type - item1.type;
	}

	public class CompareName : CompareFunction<CompareName>
	{
		// "item2" and "item1" are reversed here.
		// This is intentional!  The 'a' character is considered "less than" the 'z' character
		// Magic Storage puts A above Z in the sorting list, so the reversing here is needed.
		//  -- absoluteAquarian
		public override int Compare(Item item1, Item item2) => string.Compare(item2.Name, item1.Name, StringComparison.OrdinalIgnoreCase);
	}

	public class CompareQuantityRatio : CompareFunction<CompareQuantityRatio>
	{
		public override int Compare(Item item1, Item item2) =>
			(int)(Math.Ceiling(item1.stack / (double)item1.maxStack) * 1000 - Math.Ceiling(item2.stack / (double)item2.maxStack) * 1000);
	}

	public class CompareQuantityAbsolute : CompareFunction<CompareQuantityAbsolute>
	{
		public override int Compare(Item item1, Item item2) => item1.stack - item2.stack;
	}

	public class CompareValue : CompareFunction<CompareValue>
	{
		public override int Compare(Item item1, Item item2) => item1.value - item2.value;
	}

	public class CompareDps : CompareFunction<CompareDps>
	{
		public override int Compare(Item item1, Item item2) => (int) ((GetDps(item1) - GetDps(item2)) * 100);

		// TODO: make a more adequate implementation?  might be outside of the mod's scope
		public static double GetDps(Item item)
		{
			if (item.damage <= 0)
				return 0d;

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

	public class CompareDamage : CompareFunction<CompareDamage> {
		public override int Compare(Item item1, Item item2) => item1.damage - item2.damage;
	}
}
