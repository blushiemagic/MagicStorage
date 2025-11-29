using SerousCommonLib.API.ModCall;

namespace MagicStorage.CrossMod.Calls.Functions {
	internal class GetCampfireCondition : BaseCallFunctionNoArgs {
		// "Get Campfire Condition"

		protected override object Handle() => MagicStorageMod.HasCampfire;
	}
}
