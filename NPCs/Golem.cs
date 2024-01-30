using MagicStorage.Items;
using MagicStorage.Stations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace MagicStorage.NPCs {
	[AutoloadHead]
	internal class Golem : ModNPC {
		public bool newHelpTextAvailable;
		public bool pendingNewHelpTextCheck;

		private int newHelpTextAvailableCounter;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 25;

			// Generally for Town NPCs, but this is how the NPC does extra things such as sitting in a chair and talking to other NPCs.
			NPCID.Sets.ExtraFramesCount[Type] = 10;
			NPCID.Sets.AttackFrameCount[Type] = 4;
			// The amount of pixels away from the center of the npc that it tries to attack enemies.
			NPCID.Sets.DangerDetectRange[Type] = 4 * 16;
			NPCID.Sets.AttackType[Type] = 3;
			// The amount of time it takes for the NPC's attack animation to be over once it starts.
			NPCID.Sets.AttackTime[Type] = 20;
			NPCID.Sets.AttackAverageChance[Type] = 11;
			// For when a party is active, the party hat spawns at a Y offset.
			NPCID.Sets.HatOffsetY[Type] = -7;

			// Influences how the NPC looks in the Bestiary
			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
				Velocity = 1f,
				Direction = 1
			};

			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

			NPC.Happiness
				.SetBiomeAffection<SnowBiome>(AffectionLevel.Love)
				.SetBiomeAffection<ForestBiome>(AffectionLevel.Like)
				.SetBiomeAffection<DesertBiome>(AffectionLevel.Dislike)
				.SetBiomeAffection<HallowBiome>(AffectionLevel.Hate)
				.SetNPCAffection(NPCID.Mechanic, AffectionLevel.Love)
				.SetNPCAffection(NPCID.WitchDoctor, AffectionLevel.Like)
				.SetNPCAffection(NPCID.Wizard, AffectionLevel.Dislike)
				.SetNPCAffection(NPCID.TaxCollector, AffectionLevel.Hate);
		}

		public override void SetDefaults() {
			NPC.townNPC = true; // Sets NPC to be a Town NPC
			NPC.friendly = true; // NPC Will not attack player
			NPC.width = 24;
			NPC.height = 40;
			NPC.aiStyle = 7;
			NPC.damage = 10;
			NPC.defense = 15;
			NPC.lifeMax = 250;
			NPC.HitSound = SoundID.NPCHit41 with { Pitch = -0.61f, PitchVariance = 0.49f };
			NPC.DeathSound = SoundID.NPCDeath44 with { Pitch = 0.38f };
			NPC.knockBackResist = 0.5f;

			AnimationType = NPCID.Guide;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			// We can use AddRange instead of calling Add multiple times in order to add multiple items at once
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				// Sets the preferred biomes of this town NPC listed in the bestiary.
				// With Town NPCs, you usually set this to what biome it likes the most in regards to NPC happiness.
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Snow,

				// You can add multiple elements if you really wanted to
				// You can also use localization keys (see Localization/en-US.lang)
				new FlavorTextBestiaryInfoElement("Mods.MagicStorage.Bestiary.Golem")
			});
		}

		public override void PostAI() {
			if (Main.dedServ)
				return;

			Lighting.AddLight(NPC.Center, (Color.Orange * 0.3f).ToVector3());

			if (newHelpTextAvailable)
				newHelpTextAvailableCounter++;
			else
				newHelpTextAvailableCounter = 0;

			if (pendingNewHelpTextCheck) {
				StoragePlayer player = Main.LocalPlayer.GetModPlayer<StoragePlayer>();

				if (Utility.DownedAllMechs && !player.unlockedTip_Mechs) {
					player.unlockedTip_Mechs = true;
					newHelpTextAvailable = true;
				} else if (NPC.downedMoonlord && !player.unlockedTip_MoonLord) {
					player.unlockedTip_MoonLord = true;
					newHelpTextAvailable = true;
				}

				pendingNewHelpTextCheck = false;
				newHelpTextAvailableCounter = 0;
			}
		}

		public override void HitEffect(NPC.HitInfo hit) {
			int num = NPC.life > 0 ? 1 : 5;

			for (int k = 0; k < num; k++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.Next(new[] { DustID.Stone, DustID.Iron, DustID.WoodFurniture }));
		}

		public override bool CanTownNPCSpawn(int numTownNPCs) {
			return MagicStorageServerConfig.AllowAutomatonToMoveIn;
		}

		public override ITownNPCProfile TownNPCProfile() => new GolemProfile();

		public override List<string> SetNPCNameList()
			=> new() {
				"413-BFS",  //Beforus
				"612-ATR",  //Alternia
				"005-LEO",  //Bioweapon 05-Leo
				"001-ARC",  //Skeleton Knight, Arc
				"104-IMA",  //Iruma
				"209-TFT",  //TF2
				"557-CLD",  //FF7 Cloud
				"191-SNC",  //Sonic
				"183-MIO",  //Mario
				"109-JTR",  //Jethro, my cat -- absoluteAquarian
				"805-WLE",  //WALL-E
				"113-ATO",  //AUTO
			};

		public override string GetChat() {
			helpOption = 0;

			if (newHelpTextAvailable) {
				newHelpTextAvailable = false;
				return Language.GetTextValue("Mods.MagicStorage.Dialogue.Golem.NewTextAvailable", Main.LocalPlayer.name);
			}

			WeightedRandom<string> chat = new();

			chat.Add(Language.GetTextValue("Mods.MagicStorage.Dialogue.Golem.Greeting", Main.LocalPlayer.name), 8);
			chat.Add(Language.GetTextValue("Mods.MagicStorage.Dialogue.Golem.Bluemagic"), 2);
			chat.Add(Language.GetTextValue("Mods.MagicStorage.Dialogue.Golem.NotLihzahrd"));
			chat.Add(Language.GetTextValue("Mods.MagicStorage.Dialogue.Golem.Violence", Main.LocalPlayer.name), 5);

			if (NPC.GivenName == "805-WLE")
				chat.Add(Language.GetTextValue("Mods.MagicStorage.Dialogue.Golem.NoPlant"), 0.5);
			else if (NPC.GivenName == "113-ATO")
				chat.Add(Language.GetTextValue("Mods.MagicStorage.Dialogue.Golem.NeedPlant", Main.LocalPlayer.name), 0.5);

			int wizard = NPC.FindFirstNPC(NPCID.Wizard);
			if (wizard >= 0)
				chat.Add(Language.GetTextValue("Mods.MagicStorage.Dialogue.Golem.Wizard", Main.npc[wizard].GivenName), 3);

			return chat;
		}

		public override void SetChatButtons(ref string button, ref string button2) {
			// Sanity check
			int max = Main.netMode == NetmodeID.SinglePlayer ? HelpOptionID.Count : HelpOptionID.CountMP;

			if (helpOption < 0)
				helpOption = 0;
			else if (helpOption > max)
				helpOption = max;

			button = helpOption == 0
				? Language.GetTextValue("LegacyInterface.51")
				: helpOption > 1
					? Language.GetTextValue("Mods.MagicStorage.Dialogue.ChatOptions.Golem.PrevHelp")
					: "";


			button2 = helpOption > 0 && helpOption < max
				? Language.GetTextValue("Mods.MagicStorage.Dialogue.ChatOptions.Golem.NextHelp")
				: "";
		}

		public static class HelpOptionID {
			public const int StorageComponent = 1;
			public const int StorageHeart = 2;
			public const int StorageUnit = 3;
			public const int CraftingInterface = 4;
			public const int CraftingStationMoreFunctionality = 5;
			public const int CraftingStationMoreFunctionality2 = 6;
			public const int StorageConnector = 7;
			public const int ShadowDiamond = 8;
			public const int StorageUnitUpgrades = 9;
			public const int StorageAccess = 10;
			public const int BiomeGlobe = 11;
			public const int RemoteAccess = 12;
			public const int RemoteAccess2 = 13;
			public const int StorageUnitWand = 14;
			public const int EvilAltar = 15;
			public const int CombinedStations = 16;
			public const int CombinedStations2 = 17;
			public const int RadiantJewel = 18;
			public const int ConfigurationInterface = 19;
			public const int PortableAccesses = 20;
			public const int StorageUnitUpgrades2 = 21;
			public const int ServerOperator = 22;

			public const int Count = 21;
			public const int CountMP = Count + 1;

			public static string GetHelpKey(int id) {
				string key = "Mods.MagicStorage.Dialogue.Golem.Help";

				return key + id + id switch {
					CombinedStations2 => ModLoader.HasMod("Fargowiltas") ? "_WithFargo" : "_NoFargo",
					PortableAccesses => NPC.downedMoonlord ? "_Moon" : Utility.DownedAllMechs ? "_Mechs" : "",
					_ => ""
				};
			}

			public static string GetHelpText(int id) => Language.GetTextValue(GetHelpKey(id));

			public static int GetHelpItem(int id) {
				return id switch {
					StorageComponent => ModContent.ItemType<StorageComponent>(),
					StorageHeart => ModContent.ItemType<StorageHeart>(),
					StorageUnit or
					StorageUnitUpgrades or
					StorageUnitUpgrades2 => ModContent.ItemType<StorageUnit>(),
					CraftingInterface or
					CraftingStationMoreFunctionality or
					CraftingStationMoreFunctionality2 or
					CombinedStations or
					CombinedStations2 => ModContent.ItemType<CraftingAccess>(),
					StorageConnector => ModContent.ItemType<StorageConnector>(),
					ShadowDiamond => ModContent.ItemType<ShadowDiamond>(),
					StorageAccess => ModContent.ItemType<StorageAccess>(),
					BiomeGlobe => ModContent.ItemType<BiomeGlobe>(),
					RemoteAccess or
					RemoteAccess2 or
					PortableAccesses => ModContent.ItemType<RemoteAccess>(),
					StorageUnitWand => ModContent.ItemType<StorageDeactivator>(),
					EvilAltar => ModContent.ItemType<DemonAltar>(),
					RadiantJewel => ModContent.ItemType<RadiantJewel>(),
					ConfigurationInterface => ModContent.ItemType<EnvironmentAccess>(),
					_ => 0
				};
			}
		}

		public int helpOption;

		public static readonly int[] helpOptionsByIndex = new int[HelpOptionID.Count] {
			HelpOptionID.StorageComponent,
			HelpOptionID.StorageHeart,
			HelpOptionID.StorageUnit,
			HelpOptionID.CraftingInterface,
			HelpOptionID.CraftingStationMoreFunctionality,
			HelpOptionID.CraftingStationMoreFunctionality2,
			HelpOptionID.StorageConnector,
			HelpOptionID.ShadowDiamond,
			HelpOptionID.RadiantJewel,
			HelpOptionID.StorageUnitUpgrades,
			HelpOptionID.StorageUnitUpgrades2,
			HelpOptionID.StorageAccess,
			HelpOptionID.BiomeGlobe,
			HelpOptionID.RemoteAccess,
			HelpOptionID.RemoteAccess2,
			HelpOptionID.PortableAccesses,
			HelpOptionID.StorageUnitWand,
			HelpOptionID.EvilAltar,
			HelpOptionID.CombinedStations,
			HelpOptionID.CombinedStations2,
			HelpOptionID.ConfigurationInterface
		};

		public static readonly int[] mpHelpOptionsByIndex = helpOptionsByIndex.Prepend(HelpOptionID.ServerOperator).ToArray();

		public override void OnChatButtonClicked(bool firstButton, ref string shopName) {
			ref int savedTip = ref Main.LocalPlayer.GetModPlayer<StoragePlayer>().automatonHelpTip;

			bool wasHelpOptionUninitialized = false;
			if (helpOption == 0) {
				if (!MagicStorageConfig.DisplayLastSeenAutomatonTip)
					savedTip = 0;

				helpOption = savedTip;
				wasHelpOptionUninitialized = true;
			}

			if (!wasHelpOptionUninitialized) {
				if (firstButton)
					helpOption--;
				else
					helpOption++;
			}

			int max = Main.netMode == NetmodeID.SinglePlayer ? HelpOptionID.Count : HelpOptionID.CountMP;
			int[] helpOptionsArray = Main.netMode == NetmodeID.SinglePlayer ? helpOptionsByIndex : mpHelpOptionsByIndex;

			if (helpOption > max)
				helpOption = max;
			else if (helpOption < 1)
				helpOption = 1;

			savedTip = helpOption;

			int option = helpOptionsArray[helpOption - 1];

			Main.npcChatText = HelpOptionID.GetHelpText(option);
			Main.npcChatCornerItem = HelpOptionID.GetHelpItem(option);
		}

		// Make this Town NPC teleport to the King and/or Queen statue when triggered.
		public override bool CanGoToStatue(bool toKingStatue) => true;

		public override void TownNPCAttackStrength(ref int damage, ref float knockback) {
			damage = 20;
			knockback = 4f;
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) {
			cooldown = 30;
			randExtraCooldown = 30;
		}

		public override void DrawTownAttackSwing(ref Texture2D item, ref Rectangle itemFrame, ref int itemSize, ref float scale, ref Vector2 offset) {
			scale = 1f;

			Main.GetItemDrawFrame(ModContent.ItemType<StorageDeactivator>(), out Texture2D texture, out Rectangle frame);

			item = texture;
			itemFrame = frame;
			itemSize = itemFrame.Width;
		}

		public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight) {
			itemWidth = 48;
			itemHeight = 48;
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			float npcHeight = Main.NPCAddHeight(NPC);

			Texture2D texture = TextureAssets.Npc[Type].Value;

			Vector2 halfSize = new(texture.Width / 2, texture.Height / Main.npcFrameCount[Type] / 2);

			SpriteEffects spriteEffects = SpriteEffects.None;
			if (NPC.spriteDirection == 1)
				spriteEffects = SpriteEffects.FlipHorizontally;

			Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glow").Value;

			spriteBatch.Draw(glow,
				new Vector2(NPC.Center.X - glow.Width * NPC.scale / 2f, NPC.Bottom.Y - glow.Height * NPC.scale / Main.npcFrameCount[Type] + 4f + npcHeight + NPC.gfxOffY) - screenPos + halfSize * NPC.scale,
				NPC.frame,
				Color.White,
				NPC.rotation,
				halfSize,
				NPC.scale,
				spriteEffects,
				0f);

			if (newHelpTextAvailable) {
				Texture2D exclamation = TextureAssets.Extra[48].Value;

				Rectangle source = exclamation.Frame(8, 39, newHelpTextAvailableCounter % 60 < 30 ? 6 : 7, 1);

				Vector2 center = NPC.Top - new Vector2(0, source.Height * 0.75f) - Main.screenPosition;

				double sin = (Math.Sin(newHelpTextAvailableCounter / 60d * MathHelper.TwoPi * 0.65) + 1) / 2;

				center.Y += (float)(-5 * Math.Sin(newHelpTextAvailableCounter / 60d * MathHelper.TwoPi * 0.4));

				float transparency = (float)(0.75 + 0.25 * sin);

				spriteBatch.Draw(exclamation, center, source, Color.White * transparency, 0, source.Size() / 2f, 1.5f, SpriteEffects.None, 0);
			}
		}
	}

	public class GolemProfile : ITownNPCProfile {
		public int RollVariation() => 0;
		public string GetNameForVariant(NPC npc) => npc.getNewNPCName();

		public Asset<Texture2D> GetTextureNPCShouldUse(NPC npc) => ModContent.Request<Texture2D>("MagicStorage/NPCs/Golem");

		public int GetHeadTextureIndex(NPC npc) => ModContent.GetModHeadSlot("MagicStorage/NPCs/Golem_Head");
	}
}
