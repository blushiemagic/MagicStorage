using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader;

namespace MagicStorage.Common {
	internal static class DummyNPCPool {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				entries.Clear();
			}
		}

		private class PoolEntry {
			public readonly NPC dummy;
			public UnlockableNPCEntryIcon icon;

			public PoolEntry(int npcType) {
				dummy = new NPC();
				dummy.SetDefaults(npcType);
				dummy.IsABestiaryIconDummy = true;

				icon = new UnlockableNPCEntryIcon(npcType);
			}
		}

		private static readonly Dictionary<int, PoolEntry> entries = new();

		private static void GetOrReserve(int npcType, out NPC npc, out UnlockableNPCEntryIcon icon) {
			if (!entries.TryGetValue(npcType, out var poolEntry)) {
				poolEntry = new PoolEntry(npcType);
				entries.Add(npcType, poolEntry);
			}

			npc = poolEntry.dummy;
			npc.IsABestiaryIconDummy = true;
			icon = poolEntry.icon;
		}

		private static void DestroyIcon(int npcType) {
			if (entries.TryGetValue(npcType, out var poolEntry))
				poolEntry.icon = null;
		}

		public static void UpdateEntry(int npcType, Rectangle renderArea) {
			GetOrReserve(npcType, out _, out var icon);

			var info = new BestiaryUICollectionInfo() {
				UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
			};

			var settings = new EntryIconDrawSettings() {
				iconbox = renderArea,
				IsPortrait = true
			};

			icon?.Update(info, renderArea, settings);
		}

		public static void RenderEntry(int npcType, Rectangle renderArea) {
			GetOrReserve(npcType, out var npc, out var icon);

			if (icon is null)
				return;

			var info = new BestiaryUICollectionInfo() {
				UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1
			};

			var settings = new EntryIconDrawSettings() {
				iconbox = renderArea,
				IsPortrait = true
			};

			Rectangle clip = renderArea;
			clip.Inflate(-4, -4);

			int inflate = (int)(clip.Width * Main.UIScale) - clip.Width;
			clip.Inflate(inflate / 2, inflate / 2);

			var center = clip.Center();
			var offset = (center * Main.UIScale - center).ToPoint();
			clip.Offset(offset);

			Rectangle oldRect = Main.spriteBatch.GraphicsDevice.ScissorRectangle;
			var finalRect = Rectangle.Intersect(clip, oldRect);

			Main.spriteBatch.GraphicsDevice.ScissorRectangle = finalRect;

			float oldScale = npc.scale;

			try {
				// Shrink the NPC
				npc.scale *= Main.UIScale * (24f / 32f);
				icon.Draw(info, Main.spriteBatch, settings);
			} catch {
				DestroyIcon(npcType);
				MagicStorageMod.Instance.Logger.Error($"Failed to render NPC icon for \"{Lang.GetNPCNameValue(npcType)}\" (ID {npcType})");
			}

			npc.scale = oldScale;

			Main.spriteBatch.GraphicsDevice.ScissorRectangle = oldRect;
		}
	}
}
