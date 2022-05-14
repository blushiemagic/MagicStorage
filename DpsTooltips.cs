using System.Collections.Generic;
using MagicStorage.Sorting;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage
{
	public class DpsTooltips : GlobalItem
	{
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			if (!MagicStorageConfig.ShowDps)
				return;

			double dps = CompareDps.GetDps(item);
			if (dps > 1f)
				tooltips.Add(new TooltipLine(MagicStorage.Instance, "DPS", dps.ToString("F") + " DPS"));
		}
	}
}
