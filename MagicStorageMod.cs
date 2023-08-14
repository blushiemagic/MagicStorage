using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.RecurrentRecipes;
using MagicStorage.CrossMod;
using MagicStorage.CrossMod.Control;
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

		internal const string build144Version = "2023.6";

		public override void Load()
		{
			UsingPrivateBeta = DisplayName.Contains("BETA");

			LocalizationHelper.ForceLoadModHJsonLocalization(this);

			InterfaceHelper.Initialize();

			//Census mod support
			if (ModLoader.TryGetMod("Census", out var census)) {
				census.Call("TownNPCCondition", ModContent.NPCType<Golem>(), "No requirements");
			}

			//Sorting options
			SortingOptionLoader.Load();

			//Filtering options
			FilteringOptionLoader.Load();
		}

		public override void Unload()
		{
			CraftingGUI.Unload();
			EnvironmentGUI.Unload();

			EnvironmentModuleLoader.Unload();

			ItemCombining.combiningObjectsByType = null!;
			ItemCombining.NextID = 0;

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
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			NetHelper.HandlePacket(reader, whoAmI);
		}

		public override object Call(params object[] args) {
			if (args.Length < 1)
				throw new ArgumentException("Call requires at least one argument");

			string function = "";

			void TryParseAs<T>(int arg, out T value) {
				if (args.Length < arg + 1)
					throw new ArgumentException($"Call \"{function}\" requires at least {arg} arguments");

				if (args[arg] is T v)
					value = v;
				else
					throw new ArgumentException($"Call requires argument #{arg + 1} to be of type {typeof(T).GetSimplifiedGenericTypeName()}");
			}

			void ThrowWithMessage(string message, int argument = -1) {
				if (argument < 0)
					throw new ArgumentException($"Call \"{function}\" could not be performed.\nReason: {message}");
				else
					throw new ArgumentException($"Call \"{function}\" had an invalid value for argument #{argument}\nReason: {message}");
			}

			TryParseAs(0, out function);

			switch (function) {
				case "Prevent Shadow Diamond Drop":
					if (args.Length != 2)
						ThrowWithMessage("Expected 2 arguments");

					TryParseAs(1, out int npcID);

					if (npcID < 0)
						ThrowWithMessage("NPC ID must be positive", 1);
					else if (npcID < NPCID.Count)
						ThrowWithMessage("NPC ID must refer to a modded NPC ID", 1);

					StorageWorld.disallowDropModded.Add(npcID);
					break;
				case "Set Shadow Diamond Drop Rule":
					if (args.Length != 3)
						ThrowWithMessage("Expected 3 arguments");

					TryParseAs(1, out npcID);
					TryParseAs(2, out IItemDropRule rule);

					if (npcID < 0)
						ThrowWithMessage("NPC ID must be positive", 1);
					else if (npcID < NPCID.Count)
						ThrowWithMessage("NPC ID must refer to a modded NPC ID", 1);

					StorageWorld.moddedDiamondDropRulesByType.Add(npcID, rule);
					break;
				case "Get Shadow Diamond Drop Rule":
					if (args.Length < 2 || args.Length > 3)
						ThrowWithMessage("Expected 2 or 3 arguments");

					TryParseAs(1, out int dropNormal);
					int dropExpert = -1;

					if (args.Length == 3)
						TryParseAs(2, out dropExpert);

					if (dropNormal < 1)
						ThrowWithMessage("Normal mode drop stack must be positive");

					return ShadowDiamondDrop.DropDiamond(dropNormal, dropExpert);
				case "Get Campfire Condition":
					return HasCampfire;
				default:
					throw new ArgumentException("Call does not support the function \"" + function + "\"");
			}

			return null;
		}
	}
}
