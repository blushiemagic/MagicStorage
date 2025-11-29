using SerousCommonLib.API.ModCall;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class SimulatingCrafts : BaseCallFunctionNoArgs {
		// "Simulating Crafts"

		protected override object Handle() => CraftingGUI.SimulatingCrafts;
	}
}
