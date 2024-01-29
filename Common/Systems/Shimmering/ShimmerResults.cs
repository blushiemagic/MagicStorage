using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Common.Systems.Shimmering {
	public readonly struct TransformItem : IShimmerResult {
		IEnumerable<IShimmerResultReport> IShimmerResult.GetShimmerReports(Item item, int iconicType) {
			yield return new ItemReport(ShimmerMetrics.TransformItem(iconicType));
		}

		void IShimmerResult.OnShimmer(Item item, int iconicType, List<Item> itemSpawnOutput) {
			int result = ShimmerMetrics.TransformItem(iconicType);
			itemSpawnOutput.Add(new Item(result, item.stack));
			item.stack = 0;
		}
	}

	public readonly struct CoinLuck : IShimmerResult {
		IEnumerable<IShimmerResultReport> IShimmerResult.GetShimmerReports(Item item, int iconicType) {
			yield return new CoinLuckReport(item.stack * ItemID.Sets.CoinLuckValue[iconicType]);
		}

		void IShimmerResult.OnShimmer(Item item, int iconicType, List<Item> itemSpawnOutput) {
			Player player = Main.LocalPlayer;
			
			int coinValue = item.stack * ItemID.Sets.CoinLuckValue[iconicType];
			player.AddCoinLuck(player.Center, coinValue);
			NetMessage.SendData(MessageID.ShimmerActions, number: 1, number2: (int)player.Center.X, number3: (int)player.Center.Y, number4: coinValue);

			item.stack = 0;
		}
	}

	public readonly struct NPCSpawn : IShimmerResult {
		IEnumerable<IShimmerResultReport> IShimmerResult.GetShimmerReports(Item item, int iconicType) {
			if (iconicType == ItemID.GelBalloon)
				yield return new NPCSpawnReport(NPCID.TownSlimeRainbow);
			else if (item.makeNPC > NPCID.None)
				yield return new NPCSpawnReport(item);
		}

		void IShimmerResult.OnShimmer(Item item, int iconicType, List<Item> itemSpawnOutput) {
			Player player = Main.LocalPlayer;

			if (iconicType == ItemID.GelBalloon) {
				// Rainbow slime spawning
				if (NPC.unlockedSlimeRainbowSpawn)
					return;

				NPC.unlockedSlimeRainbowSpawn = true;
				NetMessage.SendData(MessageID.WorldData);
				int spawnedNPC = NPC.NewNPC(MagicUI.GetShimmeringSpawnSource(), (int)player.Center.X + 4, (int)player.Center.Y, NPCID.TownSlimeRainbow);
				if (spawnedNPC >= 0) {
					NPC npc = Main.npc[spawnedNPC];
					npc.velocity = Vector2.Zero;
					npc.netUpdate = true;
					npc.shimmerTransparency = 1f;
					NetMessage.SendData(MessageID.ShimmerActions, number: 2, number2: spawnedNPC);
				}

				WorldGen.CheckAchievement_RealEstateAndTownSlimes();
				item.stack--;
			} else if (item.makeNPC > NPCID.None) {
				int num10 = 50;
				int num11 = NPC.GetAvailableAmountOfNPCsToSpawnUpToSlot(item.stack, Main.maxNPCs);
				while (num10 > 0 && num11 > 0 && item.stack > 0) {
					num10--;
					num11--;
					item.stack--;

					int shimmerTransform = NPCID.Sets.ShimmerTransformToNPC[item.makeNPC];

					int spawnedNPC = shimmerTransform < 0
						? NPC.ReleaseNPC((int)player.Center.X, (int)player.Bottom.Y, item.makeNPC, item.placeStyle, Main.myPlayer)
						: NPC.ReleaseNPC((int)player.Center.X, (int)player.Bottom.Y, shimmerTransform, 0, Main.myPlayer);

					if (spawnedNPC >= 0) {
						Main.npc[spawnedNPC].shimmerTransparency = 1f;
						NetMessage.SendData(MessageID.ShimmerActions, number: 2, number2: spawnedNPC);
					}
				}
			}
		}
	}

	public readonly struct Decraft : IShimmerResult {
		public readonly int decraftingRecipeIndex;

		public Decraft(int decraftingRecipeIndex) {
			this.decraftingRecipeIndex = decraftingRecipeIndex;
		}

		IEnumerable<IShimmerResultReport> IShimmerResult.GetShimmerReports(Item item, int iconicType) {
			Recipe recipe = Main.recipe[decraftingRecipeIndex];

			var items = recipe.customShimmerResults is { } list ? list : recipe.requiredItem;

			foreach (var reqItem in items)
				yield return new ItemReport(reqItem.type);
		}

		void IShimmerResult.OnShimmer(Item item, int iconicType, List<Item> itemSpawnOutput) {
			foreach (var result in ShimmerMetrics.AttemptDecraft(Main.recipe[decraftingRecipeIndex], iconicType, ref item.stack))
				itemSpawnOutput.Add(new Item(result.type, result.stack));
		}
	}
}
