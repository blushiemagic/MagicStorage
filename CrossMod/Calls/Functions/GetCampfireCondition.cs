namespace MagicStorage.CrossMod.Calls.Functions {
	internal class GetCampfireCondition : BaseCallFunctionNoArgs {
		protected override object Handle() => MagicStorageMod.HasCampfire;
	}
}
