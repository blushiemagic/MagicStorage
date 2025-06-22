using MagicStorage.Common.Systems;
using MagicStorage.Common.Systems.Auditing;
using MagicStorage.CrossMod.Storage;
using MagicStorage.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MagicStorage.Components
{
	public class StorageUnit : StorageComponent
	{
		public override void ModifyObjectData()
		{
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleMultiplier = 6;
			TileObjectData.newTile.StyleWrapLimit = 6;
		}

		public override TEStorageUnit GetTileEntity() => ModContent.GetInstance<TEStorageUnit>();

		public override void MouseOver(int i, int j)
		{
			Main.LocalPlayer.noThrow = 2;

			base.MouseOver(i, j);
		}

		public override int ItemType(int frameX, int frameY)
		{
			return StorageUnitTierLoader.FindFromTileFrame(Type, frameX, frameY)?.StorageUnitItemType
				?? throw new Exception("No Storage Unit tier was found for this Storage Unit");
		}

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			if (Main.tile[i, j].TileFrameX % 36 == 18)
				i--;
			if (Main.tile[i, j].TileFrameY % 36 == 18)
				j--;

			if (TileEntity.ByPosition.ContainsKey(new Point16(i, j))) {
				StorageUnitTier tier = StorageUnitTierLoader.FindFromTile(i, j)
					?? throw new Exception("No Storage Unit tier was found for this Storage Unit");

				Tile tile = Main.tile[i, j];
				tier.GetState(tile.TileFrameX, tile.TileFrameY, out StorageUnitFullness fullness, out _);

				if (fullness != StorageUnitFullness.Empty)
					fail = true;
			}
		}

		public override bool CanExplode(int i, int j) {
			bool fail = false, discard = false, discard2 = false;

			KillTile(i, j, ref fail, ref discard, ref discard2);

			return !fail;
		}

		public override bool RightClick(int i, int j)
		{
			if (Main.tile[i, j].TileFrameX % 36 == 18)
				i--;
			if (Main.tile[i, j].TileFrameY % 36 == 18)
				j--;

			if (!TileEntity.ByPosition.TryGetValue(new Point16(i, j), out var te) || te is not TEStorageUnit storageUnit)
				return false;

			if (!SecuritySystem.CanPlayerAccessImmediately(Main.LocalPlayer, storageUnit.assignedNetwork)) {
				SecuritySystem.PrintStorageInaccessible();
				return true;
			}

			if (Main.LocalPlayer.HeldItem.ModItem is BaseStorageUpgradeItem upgrade && TryUpgrade(i, j, storageUnit, upgrade))
				return true;

			if (Main.LocalPlayer.HeldItem.ModItem is BaseStorageCore core && TryCoreInsertion(i, j, storageUnit, core))
				return true;

			Main.LocalPlayer.tileInteractionHappened = true;
			string activeString = storageUnit.Inactive ? Language.GetTextValue("Mods.MagicStorage.Inactive") : Language.GetTextValue("Mods.MagicStorage.Active");
			string fullnessString = Language.GetTextValue("Mods.MagicStorage.Capacity", storageUnit.NumItems, storageUnit.Capacity);
			Main.NewText(activeString + ", " + fullnessString);
			return base.RightClick(i, j);
		}

		private static bool TryCoreInsertion(int i, int j, TEStorageUnit storageUnit, BaseStorageCore core) {
			StorageUnitTier existingTier = StorageUnitTierLoader.FindFromTile(i, j)
				?? throw new Exception("No Storage Unit tier was found for this Storage Unit");

			if (existingTier.Type != StorageUnitTier.Empty.Type)
				return false;

			storageUnit.InsertCore(core);
			storageUnit.GetFramingState(out var fullness, out bool active);
			SetTypeAndStyle(i, j, core.Tier, fullness, active);
			TriggerUnitMagicAndConsumeHeldItem(i, j, storageUnit);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				AuditSystem.NetReportStorageUnitCoreInsertion(Main.myPlayer, storageUnit, core);

			return true;
		}

		private static bool TryUpgrade(int i, int j, TEStorageUnit storageUnit, BaseStorageUpgradeItem item)
		{
			StorageUnitTier existingTier = StorageUnitTierLoader.FindFromTile(i, j)
				?? throw new Exception("No Storage Unit tier was found for this Storage Unit");

			if (existingTier.CanUpgradeTo(item.Tier)) {
				storageUnit.GetFramingState(out var fullness, out bool active);
				SetTypeAndStyle(i, j, item.Tier, fullness, active);
				TriggerUnitMagicAndConsumeHeldItem(i, j, storageUnit);
				return true;
			}

			return false;
		}

		private static void TriggerUnitMagicAndConsumeHeldItem(int i, int j, TEStorageUnit storageUnit) {
			Player player = Main.LocalPlayer;
			Item item = player.HeldItem;

			storageUnit.UpdateTileFrame();
			NetMessage.SendTileSquare(Main.myPlayer, i, j, 2, 2);
			TEStorageHeart heart = storageUnit.GetHeart();
			if (heart is not null)
			{
				if (Main.netMode == NetmodeID.SinglePlayer)
					heart.ResetCompactStage();
				else if (Main.netMode == NetmodeID.MultiplayerClient)
					NetHelper.SendResetCompactStage(heart.Position);
			}

			item.stack--;
			if (item.stack <= 0)
				item.SetDefaults();
			if (player.selectedItem == 58)
				Main.mouseItem = item.Clone();

			SoundEngine.PlaySound(SoundID.MaxMana, storageUnit.Position.ToWorldCoordinates());
			Dust.NewDustPerfect(storageUnit.Position.ToWorldCoordinates(), DustID.PureSpray, Vector2.Zero, Scale: 2, newColor: Color.Green);
		}

		internal static void SetTypeAndStyle(int i, int j, StorageUnitTier tier, StorageUnitFullness fullness, bool active)
		{
			int type = tier.StorageUnitTileType;
			tier.Frame(fullness, active, out int frameX, out int frameY);

			SetTypeAndStyle(i, j, type, frameX, frameY);
			SetTypeAndStyle(i + 1, j, type, frameX + 18, frameY);
			SetTypeAndStyle(i, j + 1, type, frameX, frameY + 18);
			SetTypeAndStyle(i + 1, j + 1, type, frameX + 18, frameY + 18);
		}

		private static void SetTypeAndStyle(int i, int j, int type, int frameX, int frameY)
		{
			Tile tile = Main.tile[i, j];
			tile.TileType = (ushort)type;
			tile.TileFrameX = (short)frameX;
			tile.TileFrameY = (short)frameY;
		}

		public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
		{
			Tile tile = Main.tile[i, j];
			if (!GetGlowmask(i, j, tile.TileType, tile.TileFrameX, tile.TileFrameY, out var asset, out var color))
				return;

			Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
			Vector2 drawPos = zero + 16f * new Vector2(i, j) - Main.screenPosition;
			Rectangle frame = new(tile.TileFrameX, tile.TileFrameY, 16, 16);
			spriteBatch.Draw(asset.Value, drawPos, frame, color);
		}

		protected virtual bool GetGlowmask(int x, int y, int type, int frameX, int frameY, out Asset<Texture2D> asset, out Color drawColor) {
			asset = ModContent.Request<Texture2D>(Texture + "_Glow");
			Color lightColor = Lighting.GetColor(x, y, Color.White);
			drawColor = Color.Lerp(Color.White, lightColor, 0.5f);
			return true;
		}
	}
}
