using MagicStorage.UI.Shimmer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;

namespace MagicStorage.Common.Systems.Shimmering {
	public struct NoResultReport : IShimmerResultReport {
		public LocalizedText Label { get; }

		// Unused, no icon draws
		public readonly Asset<Texture2D> Texture => null;
		
		public object Parent { get; set; }

		public NoResultReport() {
			Label = Language.GetText("Mods.MagicStorage.DecraftingGUI.ShimmerReports.DoesNotShimmer");
		}

		public readonly bool Equals(IShimmerResultReport report) => report is NoResultReport;

		// Unused, no icon draws
		public readonly Rectangle GetAnimationFrame() => Rectangle.Empty;

		public readonly bool Render(SpriteBatch spriteBatch) => false;

		public readonly void Update() { }
	}

	public struct ItemReport : IShimmerResultReport {
		public readonly int itemType;

		public LocalizedText Label { get; }

		public Asset<Texture2D> Texture { get; }

		public object Parent { get; set; }

		public ItemReport(int itemType) {
			this.itemType = itemType;
			Label = Lang.GetItemName(itemType);
			Texture = TextureAssets.Item[itemType];
		}

		public readonly Rectangle GetAnimationFrame() {
			Main.instance.LoadItem(itemType);

			Texture2D texture = Texture.Value;
			if (Main.itemAnimations[itemType] is { } animation)
				return animation.GetFrame(texture);
			return texture.Frame();
		}

		public readonly bool Equals(IShimmerResultReport report) => report is ItemReport itemReport && itemType == itemReport.itemType;

		public readonly bool Render(SpriteBatch spriteBatch) => true;

		public readonly void Update() { }
	}

	public struct CoinLuckReport : IShimmerResultReport {
		public readonly int coinValue;

		private readonly LocalizedText _baseLabel = Language.GetText("Mods.MagicStorage.DecraftingGUI.ShimmerReports.CoinLuck");
		public readonly LocalizedText Label => GetCoinLuckAdjustment(_baseLabel);

		public Asset<Texture2D> Texture { get; }

		public object Parent { get; set; }

		public CoinLuckReport(int coinValue) {
			this.coinValue = coinValue;
			Texture = TextureAssets.Item[ItemID.GoldCoin];
		}

		private readonly LocalizedText GetCoinLuckAdjustment(LocalizedText text) {
			Player player = Main.LocalPlayer;

			float currentLuckValue = player.coinLuck;
			float adjustedLuckValue = Utils.Clamp(player.coinLuck + coinValue, 0f, 1e6f);

			float luck = player.CalculateCoinLuck(currentLuckValue);
			float adjustedLuck = player.CalculateCoinLuck(adjustedLuckValue);

			return text.WithFormatArgs(adjustedLuck - luck, adjustedLuckValue - currentLuckValue);
		}

		public readonly Rectangle GetAnimationFrame() => Texture.Frame();

		public readonly bool Equals(IShimmerResultReport report) => report is CoinLuckReport coinLuckReport && coinValue == coinLuckReport.coinValue;

		public readonly bool Render(SpriteBatch spriteBatch) => true;

		public readonly void Update() { }
	}

	public struct NPCSpawnReport : IShimmerResultReport {
		public readonly int npcType;

		public LocalizedText Label { get; }
		
		// Unused, icon is drawn manually
		public readonly Asset<Texture2D> Texture => null;

		public object Parent { get; set; }

		public NPCSpawnReport(int npcType) {
			this.npcType = npcType;
			Label = Language.GetText("Mods.MagicStorage.DecraftingGUI.ShimmerReports.NPCSpawn.Direct").WithFormatArgs(Lang.GetNPCNameValue(npcType));
		}

		public NPCSpawnReport(Item item) {
			int npc = item.makeNPC;
			int shimmerTransform = NPCID.Sets.ShimmerTransformToNPC[npc];

			npcType = shimmerTransform < 0 ? npc : shimmerTransform;

			Label = shimmerTransform < 0
				? Language.GetText("Mods.MagicStorage.DecraftingGUI.ShimmerReports.NPCSpawn.IndirectTransform").WithFormatArgs(Lang.GetNPCNameValue(npc), item.placeStyle + 1)
				: Language.GetText("Mods.MagicStorage.DecraftingGUI.ShimmerReports.NPCSpawn.DirectTransform").WithFormatArgs(Lang.GetNPCNameValue(npc));
		}

		// Unused, icon is drawn manually
		public readonly Rectangle GetAnimationFrame() => Rectangle.Empty;

		public readonly bool Equals(IShimmerResultReport report) => report is NPCSpawnReport npcSpawnReport && npcType == npcSpawnReport.npcType;

		public readonly bool Render(SpriteBatch spriteBatch) {
			if (Parent is not ShimmerReportIcon icon)
				return false;

			DummyNPCPool.RenderEntry(npcType, icon.GetDimensions().ToRectangle());
			return false;
		}

		public readonly void Update() {
			if (Parent is not ShimmerReportIcon icon)
				return;

			DummyNPCPool.UpdateEntry(npcType, icon.GetDimensions().ToRectangle());
		}
	}
}
