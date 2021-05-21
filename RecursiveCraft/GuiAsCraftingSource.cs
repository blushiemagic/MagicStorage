using RecursiveCraft;

namespace MagicStorageExtra.RecursiveCraft
{
	public class GuiAsCraftingSource : CraftingSource
	{
		public override bool[] AdjTile => CraftingGUI.adjTiles;

		public override bool AdjWater => CraftingGUI.adjWater;

		public override bool AdjHoney => CraftingGUI.adjHoney;

		public override bool AdjLava => CraftingGUI.adjLava;

		public override bool ZoneSnow => CraftingGUI.zoneSnow;

		public override bool AlchemyTable => CraftingGUI.alchemyTable;
	}
}
