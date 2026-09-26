using MagicStorage.Common.Systems.Debugging;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Common.Systems.Shimmering {
	public readonly struct TransformItem : IShimmerResult {
		public IEnumerable<IShimmerResultReport> GetShimmerReports(Item item, int iconicType) {
			int transmuted = ShimmerMetrics.TransformItem(iconicType);

			if (transmuted > ItemID.None)
				yield return new ItemReport(transmuted);
		}

		public void OnShimmer(Item item, int iconicType, StorageIntermediary storage, bool net) {
			using var debugging = DebugMessage.CreateIf(DebugControls.Names.ShimmeringRequestsVerbose);

			int result = ShimmerMetrics.TransformItem(iconicType);

			if (result <= ItemID.None) {
				// Failsafe to ensure that invalid transmutations don't do anything
				if (debugging.IsDebugging)
					debugging.Report(false, "Failed.  Item transformation resulted in an invalid or empty item.");
				return;
			}

			if (debugging.IsDebugging) {
				debugging
					.Report(false, new NetmodeContextMessage(
						ChatMessage: new("Success.  Transformed item into {0}", Utility.GetItemChatTag(result, item.stack, 0)),
						ConsoleOrLogMessage: new("Success.  Transformed item into: {0}", Utility.ItemIdentifierWithStack(result, item.stack))
					));
			}

			if (!storage.IgnoreContentChanges)
				storage.Deposit(new Item(result, item.stack));

			storage.Withdraw(item.type, item.stack);
			
			item.stack = 0;
		}

		public void Send(BinaryWriter writer) { }

		public IShimmerResult Receive(BinaryReader reader) => this;
	}

	public readonly struct CoinLuck : IShimmerResult {
		public IEnumerable<IShimmerResultReport> GetShimmerReports(Item item, int iconicType) {
			yield return new CoinLuckReport(item.stack * ItemID.Sets.CoinLuckValue[iconicType]);
		}

		public void OnShimmer(Item item, int iconicType, StorageIntermediary storage, bool net) {
			using var debugging = DebugMessage.CreateIf(DebugControls.Names.ShimmeringRequestsVerbose);

			int coinValue = item.stack * ItemID.Sets.CoinLuckValue[iconicType];

			if (debugging.IsDebugging)
				debugging.Report(false, "Adding {0} value to the coin luck counter", coinValue);

			if (Main.netMode == NetmodeID.SinglePlayer)
				Main.LocalPlayer.AddCoinLuck(storage.playerCenter, coinValue);  // Add coin luck immediately
			
			if (!net)
				NetMessage.SendData(MessageID.ShimmerActions, number: 1, number2: (int)storage.playerCenter.X, number3: (int)storage.playerCenter.Y, number4: coinValue);

			storage.Withdraw(item.type, item.stack);

			item.stack = 0;
		}

		public void Send(BinaryWriter writer) { }

		public IShimmerResult Receive(BinaryReader reader) => this;
	}

	public readonly struct NPCSpawn : IShimmerResult {
		public IEnumerable<IShimmerResultReport> GetShimmerReports(Item item, int iconicType) {
			if (iconicType == ItemID.GelBalloon)
				yield return new NPCSpawnReport(NPCID.TownSlimeRainbow);
			else if (item.makeNPC > NPCID.None)
				yield return new NPCSpawnReport(item);
		}

		public void OnShimmer(Item item, int iconicType, StorageIntermediary storage, bool net) {
			using var debugging = DebugMessage.CreateIf(DebugControls.Names.ShimmeringRequestsVerbose);

			if (iconicType == ItemID.GelBalloon) {
				// Rainbow slime spawning
				if (NPC.unlockedSlimeRainbowSpawn) {
					if (debugging.IsDebugging)
						debugging.Report(false, "Skipped.  Diva Slime has already been unlocked.");

					return;
				}

				if (debugging.IsDebugging)
					debugging.Report(false, "Success.  Unlocking the Diva Slime.");

				if (!net) {
					NPC.unlockedSlimeRainbowSpawn = true;
					NetMessage.SendData(MessageID.WorldData);
					int spawnedNPC = NPC.NewNPC(MagicUI.GetShimmeringSpawnSource(), (int)storage.playerCenter.X + 4, (int)storage.playerCenter.Y, NPCID.TownSlimeRainbow);
					if (spawnedNPC >= 0) {
						NPC npc = Main.npc[spawnedNPC];
						npc.velocity = Vector2.Zero;
						npc.netUpdate = true;
						npc.shimmerTransparency = 1f;
						NetMessage.SendData(MessageID.ShimmerActions, number: 2, number2: spawnedNPC);
					}

					WorldGen.CheckAchievement_RealEstateAndTownSlimes();
				}
				
				storage.Withdraw(item.type);

				item.stack--;
			} else if (item.makeNPC > NPCID.None) {
				int num10 = 50;
				int num11 = NPC.GetAvailableAmountOfNPCsToSpawnUpToSlot(item.stack, Main.maxNPCs);
				int count = 0;
				int countNPC = 0;

				int shimmerTransform = NPCID.Sets.ShimmerTransformToNPC[item.makeNPC];

				if (num11 > 0 && debugging.IsDebugging) {
					debugging.Report(false, "Spawned NPC: {0}", NPCID.Search.GetName(shimmerTransform < 0 ? item.makeNPC : shimmerTransform));

					if (shimmerTransform < 0)
						debugging.Report(false, "NPC Style: {0}", item.placeStyle);
				}

				while (num10 > 0 && num11 > 0 && item.stack > 0) {
					num10--;
					num11--;
					item.stack--;
					count++;

					if (!net) {
						int spawnedNPC = shimmerTransform < 0
							? NPC.ReleaseNPC((int)storage.playerBottom.X, (int)storage.playerBottom.Y, item.makeNPC, item.placeStyle, Main.myPlayer)
							: NPC.ReleaseNPC((int)storage.playerBottom.X, (int)storage.playerBottom.Y, shimmerTransform, 0, Main.myPlayer);

						if (spawnedNPC >= 0) {
							countNPC++;

							NPC npc = Main.npc[spawnedNPC];

							npc.shimmerTransparency = 1f;
							NetMessage.SendData(MessageID.ShimmerActions, number: 2, number2: spawnedNPC);

							// NPC is supposed to get shimmered here... but the player could be anywhere!
							// Force the shimmer to happen
							npc.shimmering = true;
							npc.GetShimmered();
						}
					}
				}

				if (!net && debugging.IsDebugging) {
					debugging
						.Report(false, "Consumed {0} items", count)
						.Report(false, "Spawned {0} NPCs", countNPC);
				}

				storage.Withdraw(item.type, count);
			}
		}

		public void Send(BinaryWriter writer) { }

		public IShimmerResult Receive(BinaryReader reader) => this;
	}

	public readonly struct Decraft : IShimmerResult {
		public readonly int decraftingRecipeIndex;

		public Decraft(int decraftingRecipeIndex) {
			this.decraftingRecipeIndex = decraftingRecipeIndex;
		}

		public IEnumerable<IShimmerResultReport> GetShimmerReports(Item item, int iconicType) {
			Recipe recipe = Main.recipe[decraftingRecipeIndex];

			var items = recipe.customShimmerResults is { } list ? list : recipe.requiredItem;

			foreach (var reqItem in items)
				yield return new ItemReport(reqItem.type);
		}

		public void OnShimmer(Item item, int iconicType, StorageIntermediary storage, bool net) {
			int oldStack = item.stack;

			foreach (var result in ShimmerMetrics.AttemptDecraft(Main.recipe[decraftingRecipeIndex], iconicType, ref item.stack)) {
				if (!storage.IgnoreContentChanges)
					storage.Deposit(new Item(result.type, result.stack));
			}

			storage.Withdraw(item.type, oldStack - item.stack);
		}

		public void Send(BinaryWriter writer) {
			writer.Write(decraftingRecipeIndex);
		}

		public IShimmerResult Receive(BinaryReader reader) {
			return new Decraft(reader.ReadInt32());
		}
	}
}
