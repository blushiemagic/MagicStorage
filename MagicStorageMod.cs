using MagicStorage.Common.Systems.Debugging;
using MagicStorage.CrossMod;
using MagicStorage.CrossMod.Control;
using MagicStorage.CrossMod.Storage;
using SerousCommonLib.API.Helpers;
using SerousCommonLib.API.ModCall;
using System.IO;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage {
	public class MagicStorageMod : Mod {
		public static MagicStorageMod Instance => ModContent.GetInstance<MagicStorageMod>();

		internal static bool UsingPrivateBeta { get; private set; }  //Make sure to add the "NETPLAY" define when setting this to true for beta builds! -- absoluteAquarian

		// Integration with ModHelpers
		public static string GithubUserName => "blushiemagic";
		public static string GithubProjectName => "MagicStorage";

		public static readonly Condition HasCampfire = new(Language.GetText("Mods.MagicStorage.CookedMarshmallowCondition"), () => CraftingGUI.Campfire);

		public UIOptionConfigurationManager optionsConfig;
		internal DebuggingConfigurationManager debugConfig;

		public MagicStorageMod() {
			PreJITFilter = new CheckModBuildVersionBeforeJIT();
			CheckModBuildVersionBeforeJIT.Mod = this;
		}

		public override void Load()
		{
			debugConfig = new();
			debugConfig.LoadConfigurations();

			UsingPrivateBeta = DisplayName.Contains("BETA");

			LocalizationHelper.ForceLoadModHJsonLocalization(this);

			InterfaceHelper.Initialize();

			//Sorting options
			SortingOptionLoader.Load();

			//Filtering options
			FilteringOptionLoader.Load();
		}

		public override void Unload()
		{
			StorageGUI.Unload();
			CraftingGUI.Unload();
			EnvironmentGUI.Unload();
			DecraftingGUI.Unload();

			EnvironmentModuleLoader.Unload();

			SortingOptionLoader.Unload();
			FilteringOptionLoader.Unload();

			optionsConfig = null;
			debugConfig = null;

			CheckModBuildVersionBeforeJIT.Mod = null;
			CheckModBuildVersionBeforeJIT.versionChecked = false;
		}

		public override void PostSetupContent() {
			if (!Main.dedServ) {
				optionsConfig = new();
				optionsConfig.Initialize();
			}

			SortingOptionLoader.InitializeOrder();
			FilteringOptionLoader.InitializeOrder();
			StorageTierModifierLoader.PostSetupContent();
			// NOTE: StorageTierModifier will not work properly after this call, hence why its loader's method is called first
			StorageUnitTierLoader.PostSetupContent();
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			NetHelper.HandlePacket(reader, whoAmI);
		}

		public override object Call(params object[] args) => BaseCallFunction.Call(this, args);
	}
}
