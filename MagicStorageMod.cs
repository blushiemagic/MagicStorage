using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.CrossMod;
using MagicStorage.CrossMod.Calls;
using MagicStorage.CrossMod.Control;
using MagicStorage.CrossMod.Storage;
using MagicStorage.Items;
using MagicStorage.NPCs;
using MagicStorage.Stations;
using SerousCommonLib.API;
using SerousCommonLib.API.Helpers;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
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

			Obsolete_Unload();

			SortingOptionLoader.Unload();
			FilteringOptionLoader.Unload();

			optionsConfig = null;

			CheckModBuildVersionBeforeJIT.Mod = null;
			CheckModBuildVersionBeforeJIT.versionChecked = false;
		}

		[Obsolete]
		private static void Obsolete_Unload() {
			ItemCombining.NextID = 0;
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

		public override object Call(params object[] args) {
			if (args.Length < 1)
				throw new ArgumentException("Call requires at least one argument");

			if (args[0] is not string function)
				throw new ArgumentException("Expected function name");

			return BaseCallFunction.Find(this, function).Call(args.AsSpan(1));
		}
	}
}
