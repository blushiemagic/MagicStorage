using MagicStorage.CrossMod;
using MagicStorage.CrossMod.Control;
using MagicStorage.CrossMod.Storage;
using SerousCommonLib.API.Helpers;
using SerousCommonLib.API.ModCall;
using System;
using System.IO;
using System.Reflection;
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

		public MagicStorageMod() {
			PreJITFilter = new CheckModBuildVersionBeforeJIT();
			CheckModBuildVersionBeforeJIT.Mod = this;
		}

		internal const string build144Version = "2023.8";

		public override void Load()
		{
			UsingPrivateBeta = DisplayName.Contains("BETA");

			// Load localization first to ensure it's available
			LocalizationHelper.ForceLoadModHJsonLocalization(this);

			// Load NPinyin.Core.dll from embedded resources (after localization)
			LoadNPinyinAssembly();

			InterfaceHelper.Initialize();

			//Sorting options
			SortingOptionLoader.Load();

			//Filtering options
			FilteringOptionLoader.Load();
		}

		/// <summary>
		/// Reference to the loaded NPinyin.Core assembly
		/// </summary>
		public static Assembly NPinyinAssembly { get; private set; }

		/// <summary>
		/// Load NPinyin.Core.dll from embedded resources
		/// </summary>
		private void LoadNPinyinAssembly() {
			try {
				// Get the current assembly
				Assembly assembly = Assembly.GetExecutingAssembly();
				string resourceName = "NPinyin.Core.dll";

				// Read DLL from embedded resources
				using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
					if (stream == null) {
						Logger.Warn($"Could not find embedded resource: {resourceName}. Pinyin search will be disabled.");
						return;
					}

					// Read DLL byte array
					byte[] assemblyData = new byte[stream.Length];
					stream.Read(assemblyData, 0, assemblyData.Length);

					// Load assembly and save reference
					NPinyinAssembly = Assembly.Load(assemblyData);
					
					// Mark NPinyin as loaded, allowing JIT compilation of ConvertToPinyin method
					CheckModBuildVersionBeforeJIT.nPinyinLoaded = true;
					
					Logger.Info("Successfully loaded NPinyin.Core.dll from embedded resources.");
				}
			} catch (Exception ex) {
				// If loading fails, log error but don't affect other mod functionality
				Logger.Warn($"Failed to load NPinyin.Core.dll: {ex.Message}. Pinyin search will be disabled.");
			}
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
			StorageUnitTierLoader.PostSetupContent();
			StorageTierModifierLoader.PostSetupContent();
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			NetHelper.HandlePacket(reader, whoAmI);
		}

		public override object Call(params object[] args) => BaseCallFunction.Call(this, args);
	}
}
