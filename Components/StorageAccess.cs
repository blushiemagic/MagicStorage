using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MagicStorage.Components {
	public class StorageAccess : StorageComponent {
		public override int ItemType(int frameX, int frameY) => ModContent.ItemType<Items.StorageAccess>();

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

		public virtual TEStorageHeart GetHeart(int i, int j) {
			Point16 point = TEStorageComponent.FindStorageCenter(new Point16(i, j));
			if (point.X < 0 || point.Y < 0)
				return null;

			if (TileEntity.ByPosition.TryGetValue(point, out TileEntity te) && te is TEStorageCenter center)
				return center.GetHeart();

			return null;
		}

		public override void MouseOver(int i, int j) {
			Player player = Main.LocalPlayer;
			Tile tile = Main.tile[i, j];
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ItemType(tile.TileFrameX, tile.TileFrameY);
			player.noThrow = 2;
		}

		public override bool RightClick(int i, int j) {
			if (Main.tile[i, j].TileFrameX % 36 == 18)
				i--;
			if (Main.tile[i, j].TileFrameY % 36 == 18)
				j--;

			// the translation key is in en-US.hjson  -Crapsky233
			string text = Language.GetTextValue($"Mods.MagicStorage.StorageAccessFail{(this is StorageHeart ? "" : "Alt")}");
			if (TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity tileEntity))
				if (tileEntity is TERemoteAccess remoteAccess && !remoteAccess.Loaded)
					text = Language.GetTextValue($"Mods.MagicStorage.StorageAccessFailLoad");

			if (GetHeart(i, j) == null)
			{
				Main.NewText(text);
				return true;
			}

			Player player = Main.LocalPlayer;
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			Main.mouseRightRelease = false;
			if (player.sign > -1)
			{
				SoundEngine.PlaySound(SoundID.MenuClose);
				player.sign = -1;
				Main.editSign = false;
				Main.npcChatText = string.Empty;
			}

			if (Main.editChest)
			{
				SoundEngine.PlaySound(SoundID.MenuTick);
				Main.editChest = false;
				Main.npcChatText = string.Empty;
			}

			if (player.editedChestName)
			{
				NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f);
				player.editedChestName = false;
			}

			if (player.talkNPC > -1)
			{
				player.SetTalkNPC(-1);
				Main.npcChatCornerItem = 0;
				Main.npcChatText = string.Empty;
			}

			bool hadChestOpen = player.chest != -1;
			player.chest = -1;
			Main.stackSplit = 600;
			Point16 toOpen = new(i, j);
			Point16 prevOpen = modPlayer.ViewingStorage();
			if (prevOpen == toOpen)
			{
				modPlayer.CloseStorage();
				SoundEngine.PlaySound(SoundID.MenuClose);
				Recipe.FindRecipes();
			}
			else
			{
				bool hadOtherOpen = prevOpen.X >= 0 && prevOpen.Y >= 0;
				modPlayer.OpenStorage(toOpen);
				modPlayer.timeSinceOpen = 0;
				if (PlayerInput.GrappleAndInteractAreShared)
					PlayerInput.Triggers.JustPressed.Grapple = false;
				Main.recBigList = false;
				SoundEngine.PlaySound(hadChestOpen || hadOtherOpen ? SoundID.MenuTick : SoundID.MenuOpen);
				Recipe.FindRecipes();
			}

			return true;
		}

		public override void PostDraw(int i, int j, SpriteBatch spriteBatch) {
			Tile tile = Main.tile[i, j];
			Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
			Vector2 drawPos = zero + 16f * new Vector2(i, j) - Main.screenPosition;
			Rectangle frame = new(tile.TileFrameX, tile.TileFrameY, 16, 16);
			Color lightColor = Lighting.GetColor(i, j, Color.White);
			Color color = Color.Lerp(lightColor, Color.White, Main.essScale);
			spriteBatch.Draw(Mod.Assets.Request<Texture2D>("Components/" + Name + "_Glow").Value, drawPos, frame, color);
		}
	}
}
